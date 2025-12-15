using OlistGertecIntegration.Models;
using OlistGertecIntegration.Services;

namespace OlistGertecIntegration.Services;

public class IntegrationService : BackgroundService
{
    private readonly ILogger<IntegrationService> _logger;
    private readonly GertecProtocolService _gertecService;
    private readonly OlistApiService _olistService;
    private readonly IConfiguration _configuration;
    private DateTime? _lastSyncDate;
    private readonly Dictionary<string, Produto> _productCache = new();

    public IntegrationService(
        ILogger<IntegrationService> logger,
        GertecProtocolService gertecService,
        OlistApiService olistService,
        IConfiguration configuration)
    {
        _logger = logger;
        _gertecService = gertecService;
        _olistService = olistService;
        _configuration = configuration;
        
        // Configura evento de código de barras recebido
        _gertecService.BarcodeReceived += OnBarcodeReceived;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de integração iniciado");

        // Conecta ao Gertec
        await _gertecService.ConnectAsync();

        // Inicia monitoramento de preços se habilitado
        var monitoringEnabled = _configuration.GetValue<bool>("PriceMonitoring:Enabled");
        if (monitoringEnabled)
        {
            _ = Task.Run(() => MonitorPriceChangesAsync(stoppingToken), stoppingToken);
        }

        // Mantém o serviço rodando e reconecta se necessário
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_gertecService.IsConnected)
            {
                _logger.LogWarning("Conexão com Gertec perdida. Tentando reconectar...");
                await _gertecService.ConnectAsync();
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    private async void OnBarcodeReceived(object? sender, string barcode)
    {
        try
        {
            _logger.LogInformation($"Processando código de barras: {barcode}");

            // Busca produto no Olist
            var produto = await _olistService.GetProductByBarcodeAsync(barcode);

            if (produto != null)
            {
                // Verifica se produto tem imagem
                var imageUrl = produto.GetImageUrl();
                
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    try
                    {
                        // Baixa e converte imagem
                        var imageData = await _olistService.DownloadAndConvertImageAsync(imageUrl);
                        
                        if (imageData != null && imageData.Length > 0)
                        {
                            // Envia imagem ao Gertec
                            var imageSent = await _gertecService.SendImageGifAsync(imageData, index: 0, loops: 1, durationSeconds: 5);
                            
                            if (imageSent)
                            {
                                _logger.LogInformation($"Imagem do produto enviada ao Gertec: {produto.Nome}");
                                
                                // Após enviar imagem, também envia nome e preço como texto
                                // (alguns modelos podem exibir ambos)
                                var nomeFormatado = FormatProductName(produto.Nome);
                                var preco = !string.IsNullOrEmpty(produto.PrecoPromocional) && 
                                           decimal.TryParse(produto.PrecoPromocional, out var precoPromo) && 
                                           precoPromo > 0
                                    ? produto.PrecoPromocional 
                                    : produto.Preco;
                                var precoFormatado = _olistService.FormatPrice(preco);
                                
                                // Aguarda um pouco antes de enviar texto
                                await Task.Delay(500);
                                await _gertecService.SendProductInfoAsync(nomeFormatado, precoFormatado);
                            }
                            else
                            {
                                // Fallback: se falhar ao enviar imagem, envia apenas texto
                                _logger.LogWarning("Falha ao enviar imagem. Enviando apenas texto.");
                                await SendProductTextOnlyAsync(produto);
                            }
                        }
                        else
                        {
                            // Fallback: se não conseguir baixar imagem, envia apenas texto
                            _logger.LogWarning("Não foi possível baixar/converter imagem. Enviando apenas texto.");
                            await SendProductTextOnlyAsync(produto);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar imagem do produto. Enviando apenas texto.");
                        await SendProductTextOnlyAsync(produto);
                    }
                }
                else
                {
                    // Produto sem imagem: envia apenas texto
                    await SendProductTextOnlyAsync(produto);
                }
                
                // Atualiza cache
                _productCache[barcode] = produto;
            }
            else
            {
                _logger.LogWarning($"Produto não encontrado para código: {barcode}");
                await _gertecService.SendProductNotFoundAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao processar código de barras: {barcode}");
            await _gertecService.SendProductNotFoundAsync();
        }
    }

    private async Task SendProductTextOnlyAsync(Produto produto)
    {
        // Formata nome para 4 linhas x 20 colunas (80 bytes)
        var nomeFormatado = FormatProductName(produto.Nome);
        
        // Usa preço promocional se disponível e maior que zero, senão usa preço normal
        var preco = !string.IsNullOrEmpty(produto.PrecoPromocional) && 
                   decimal.TryParse(produto.PrecoPromocional, out var precoPromo) && 
                   precoPromo > 0
            ? produto.PrecoPromocional 
            : produto.Preco;
        
        var precoFormatado = _olistService.FormatPrice(preco);

        // Envia para o Gertec
        await _gertecService.SendProductInfoAsync(nomeFormatado, precoFormatado);
    }

    private string FormatProductName(string nome)
    {
        // Formata para 4 linhas x 20 colunas (80 bytes total)
        // Divide o nome em até 4 linhas de 20 caracteres cada
        var linhas = new List<string>();
        var nomeLimpo = nome.Replace("\n", " ").Replace("\r", "");

        for (int i = 0; i < 4 && i * 20 < nomeLimpo.Length; i++)
        {
            var linha = nomeLimpo.Substring(i * 20, Math.Min(20, nomeLimpo.Length - i * 20));
            linhas.Add(linha);
        }

        // Preenche até 4 linhas
        while (linhas.Count < 4)
        {
            linhas.Add(new string(' ', 20));
        }

        return string.Join("", linhas);
    }

    private async Task MonitorPriceChangesAsync(CancellationToken cancellationToken)
    {
        var intervalMinutes = _configuration.GetValue<int>("PriceMonitoring:CheckIntervalMinutes", 5);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Verificando mudanças de preços...");

                var produtos = await _olistService.GetAllProductsAsync(_lastSyncDate);

                foreach (var produto in produtos)
                {
                    // Usa código ou GTIN como chave
                    var chaveProduto = !string.IsNullOrEmpty(produto.Codigo) 
                        ? produto.Codigo 
                        : produto.Gtin;
                    
                    if (string.IsNullOrEmpty(chaveProduto))
                        continue;

                    // Verifica se o preço mudou
                    if (_productCache.TryGetValue(chaveProduto, out var produtoCache))
                    {
                        if (produtoCache.Preco != produto.Preco || 
                            produtoCache.PrecoPromocional != produto.PrecoPromocional)
                        {
                            _logger.LogInformation($"Preço alterado para produto {produto.Nome} (Código: {chaveProduto})");
                            
                            // Atualiza cache
                            _productCache[chaveProduto] = produto;
                            
                            // Opcional: Enviar mensagem ao Gertec sobre atualização
                            await _gertecService.SendMessageAsync(
                                "Precos atualizados", 
                                $"Produto: {produto.Nome.Substring(0, Math.Min(20, produto.Nome.Length))}", 
                                3);
                        }
                    }
                    else
                    {
                        // Novo produto
                        _productCache[chaveProduto] = produto;
                    }
                }

                _lastSyncDate = DateTime.Now;
                _logger.LogInformation($"Monitoramento concluído. Próxima verificação em {intervalMinutes} minutos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no monitoramento de preços");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), cancellationToken);
        }
    }

    public override void Dispose()
    {
        _gertecService.BarcodeReceived -= OnBarcodeReceived;
        base.Dispose();
    }
}


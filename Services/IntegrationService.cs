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

        // Pré-carrega cache de produtos na inicialização (sua ideia!)
        _logger.LogInformation("Pré-carregando cache de produtos...");
        try
        {
            await _olistService.PreloadCacheAsync();
            _logger.LogInformation("Cache de produtos pré-carregado com sucesso!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao pré-carregar cache. Continuando sem cache pré-carregado...");
        }

        // Conecta ao Gertec
        await _gertecService.ConnectAsync();

        // Inicia monitoramento de preços se habilitado
        var monitoringEnabled = _configuration.GetValue<bool>("PriceMonitoring:Enabled");
        if (monitoringEnabled)
        {
            _ = Task.Run(() => MonitorPriceChangesAsync(stoppingToken), stoppingToken);
        }

        // Mantém o serviço rodando e reconecta se necessário
        int tentativasConsecutivas = 0;
        const int maxTentativasAntesDelay = 3;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_gertecService.IsConnected)
            {
                _logger.LogWarning("Conexão com Gertec perdida. Tentando reconectar...");
                
                var conectado = await _gertecService.ConnectAsync();
                
                if (conectado)
                {
                    tentativasConsecutivas = 0;
                    _logger.LogInformation("Reconectado ao Gertec com sucesso");
                }
                else
                {
                    tentativasConsecutivas++;
                    
                    // Após várias tentativas falhas, aumenta o intervalo entre tentativas
                    if (tentativasConsecutivas >= maxTentativasAntesDelay)
                    {
                        var reconnectInterval = _configuration.GetValue<int>("Gertec:ReconnectIntervalSeconds", 5);
                        var delayAumentado = reconnectInterval * tentativasConsecutivas;
                        _logger.LogWarning($"Múltiplas tentativas de reconexão falharam. Aguardando {delayAumentado} segundos antes da próxima tentativa...");
                        await Task.Delay(TimeSpan.FromSeconds(delayAumentado), stoppingToken);
                    }
                    else
                    {
                        await Task.Delay(5000, stoppingToken);
                    }
                }
            }
            else
            {
                tentativasConsecutivas = 0;
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async void OnBarcodeReceived(object? sender, string barcode)
    {
        try
        {
            _logger.LogInformation($"Processando código de barras: {barcode}");

            // Envia mensagem de "processando" imediatamente para evitar "conexão falhou" no terminal
            // Isso garante que o terminal saiba que recebeu o código e está processando
            // Tempo aumentado para 5 segundos para cobrir busca completa (GTIN pode demorar 3-4s)
            if (_gertecService.IsConnected)
            {
                await _gertecService.SendMessageAsync("Consultando...", "Aguarde", 5);
            }

            // Busca produto no Olist
            var produto = await _olistService.GetProductByBarcodeAsync(barcode);

            if (produto != null)
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

                // Envia imagem do produto se disponível (antes de enviar nome e preço)
                if (!string.IsNullOrEmpty(produto.Imagem) || !string.IsNullOrEmpty(produto.ImagemPrincipal))
                {
                    try
                    {
                        var imagemUrl = produto.ImagemPrincipal ?? produto.Imagem;
                        if (!string.IsNullOrEmpty(imagemUrl))
                        {
                            _logger.LogInformation($"Enviando imagem do produto: {imagemUrl}");
                            await _gertecService.SendImageFromFileAsync(imagemUrl, indice: 0, numeroLoops: 1, tempoExibicao: 5);
                        }
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogWarning(imgEx, "Erro ao enviar imagem do produto. Continuando com nome e preço...");
                    }
                }

                // Envia nome e preço para o Gertec
                await _gertecService.SendProductInfoAsync(nomeFormatado, precoFormatado);
                
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
        
        // Sincronização inicial completa na primeira execução
        bool primeiraExecucao = true;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Verificando mudanças de preços no Tiny ERP...");

                // Busca todos os produtos do Tiny ERP
                var produtos = await _olistService.GetAllProductsAsync(primeiraExecucao ? null : _lastSyncDate);
                
                if (primeiraExecucao)
                {
                    _logger.LogInformation($"Sincronização inicial: {produtos.Count} produtos encontrados no Tiny ERP");
                }
                else
                {
                    _logger.LogInformation($"{produtos.Count} produtos verificados desde a última sincronização");
                }

                int produtosAtualizados = 0;
                int produtosNovos = 0;

                foreach (var produto in produtos)
                {
                    // Usa código ou GTIN como chave
                    var chaveProduto = !string.IsNullOrEmpty(produto.Codigo) 
                        ? produto.Codigo 
                        : produto.Gtin;
                    
                    if (string.IsNullOrEmpty(chaveProduto))
                        continue;

                    // Verifica se o preço mudou ou se é um novo produto
                    if (_productCache.TryGetValue(chaveProduto, out var produtoCache))
                    {
                        // Verifica se houve mudança de preço
                        bool precoMudou = produtoCache.Preco != produto.Preco || 
                                         produtoCache.PrecoPromocional != produto.PrecoPromocional;
                        
                        if (precoMudou)
                        {
                            _logger.LogInformation($"Preço alterado para produto {produto.Nome} (Código: {chaveProduto}) - " +
                                                  $"Preço anterior: {produtoCache.Preco}, Novo preço: {produto.Preco}");
                            
                            // Atualiza cache local
                            _productCache[chaveProduto] = produto;
                            
                            // Envia informações atualizadas para o Gertec
                            if (_gertecService.IsConnected)
                            {
                                try
                                {
                                    var nomeFormatado = FormatProductName(produto.Nome);
                                    
                                    // Usa preço promocional se disponível e maior que zero, senão usa preço normal
                                    var preco = !string.IsNullOrEmpty(produto.PrecoPromocional) && 
                                               decimal.TryParse(produto.PrecoPromocional, out var precoPromo) && 
                                               precoPromo > 0
                                        ? produto.PrecoPromocional 
                                        : produto.Preco;
                                    
                                    var precoFormatado = _olistService.FormatPrice(preco);
                                    
                                    // Envia produto atualizado para o Gertec
                                    bool enviado = await _gertecService.SendProductInfoAsync(nomeFormatado, precoFormatado);
                                    
                                    if (enviado)
                                    {
                                        _logger.LogInformation($"Produto {produto.Nome} atualizado no Gertec com sucesso");
                                        produtosAtualizados++;
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"Falha ao enviar produto {produto.Nome} para o Gertec");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Erro ao enviar produto {produto.Nome} para o Gertec");
                                }
                            }
                            else
                            {
                                _logger.LogWarning($"Gertec não conectado. Produto {produto.Nome} não foi atualizado");
                            }
                        }
                    }
                    else
                    {
                        // Novo produto - adiciona ao cache
                        _productCache[chaveProduto] = produto;
                        produtosNovos++;
                        
                        if (primeiraExecucao)
                        {
                            _logger.LogDebug($"Novo produto adicionado ao cache: {produto.Nome} (Código: {chaveProduto})");
                        }
                        else
                        {
                            _logger.LogInformation($"Novo produto detectado: {produto.Nome} (Código: {chaveProduto})");
                        }
                    }
                }

                _lastSyncDate = DateTime.Now;
                
                // Atualiza cache completo do OlistApiService quando detecta mudanças ou novos produtos
                if (produtosAtualizados > 0 || produtosNovos > 0 || primeiraExecucao)
                {
                    _logger.LogInformation("Atualizando cache completo do OlistApiService...");
                    try
                    {
                        await _olistService.RefreshCacheAsync();
                        _logger.LogInformation("Cache do OlistApiService atualizado com sucesso!");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao atualizar cache do OlistApiService");
                    }
                }
                
                if (primeiraExecucao)
                {
                    _logger.LogInformation($"Sincronização inicial concluída: {produtos.Count} produtos no cache. " +
                                          $"Próxima verificação em {intervalMinutes} minutos.");
                    primeiraExecucao = false;
                }
                else
                {
                    _logger.LogInformation($"Monitoramento concluído: {produtosAtualizados} produtos atualizados, " +
                                          $"{produtosNovos} produtos novos. Cache atualizado. Próxima verificação em {intervalMinutes} minutos.");
                }
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


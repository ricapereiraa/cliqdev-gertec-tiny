using OlistGertecIntegration.Models;
using OlistGertecIntegration.Services;

namespace OlistGertecIntegration.Services;

public class IntegrationService : BackgroundService
{
    private readonly ILogger<IntegrationService> _logger;
    private readonly GertecProtocolService _gertecService;
    private readonly OlistApiService _olistService;
    private readonly GertecDataFileService _dataFileService;
    private readonly IConfiguration _configuration;
    private DateTime? _lastSyncDate;
    private readonly Dictionary<string, Produto> _productCache = new();

    public IntegrationService(
        ILogger<IntegrationService> logger,
        GertecProtocolService gertecService,
        OlistApiService olistService,
        GertecDataFileService dataFileService,
        IConfiguration configuration)
    {
        _logger = logger;
        _gertecService = gertecService;
        _olistService = olistService;
        _dataFileService = dataFileService;
        _configuration = configuration;
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

        // Gera arquivo TXT inicial para servidor TCP oficial do Gertec
        _logger.LogInformation("Gerando arquivo de dados inicial para servidor TCP Gertec...");
        Console.WriteLine("Gerando arquivo de dados inicial para servidor TCP Gertec...");
        try
        {
            await _dataFileService.GenerateDataFileAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar arquivo de dados inicial. Continuando...");
        }

        // Conecta ao servidor Gertec
        _logger.LogInformation("Conectando ao servidor Gertec...");
        Console.WriteLine("Conectando ao servidor Gertec...");
        var connected = await _gertecService.ConnectAsync();
        if (connected)
        {
            _logger.LogInformation("Conexão estabelecida com servidor Gertec");
            Console.WriteLine("Conexão estabelecida com servidor Gertec");
        }
        else
        {
            _logger.LogWarning("Falha ao conectar ao servidor Gertec. Tentando novamente em 5 segundos...");
            Console.WriteLine("Falha ao conectar ao servidor Gertec. Tentando novamente em 5 segundos...");
        }

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
                _logger.LogWarning("Conexão perdida com servidor Gertec. Tentando reconectar...");
                await _gertecService.ReconnectAsync();
            }
            
            await Task.Delay(5000, stoppingToken);
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
                Console.WriteLine("VERIFICANDO se há novos produtos ou mudanças de preços no Tiny ERP...");

                // Busca todos os produtos do Tiny ERP
                var produtos = await _olistService.GetAllProductsAsync(primeiraExecucao ? null : _lastSyncDate);
                
                if (primeiraExecucao)
                {
                    _logger.LogInformation($"Sincronização inicial: {produtos.Count} produtos encontrados no Tiny ERP");
                }
                else
                {
                    _logger.LogInformation($"{produtos.Count} produtos verificados desde a última sincronização");
                    Console.WriteLine($"{produtos.Count} produtos verificados desde a última sincronização");
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
                            
                            // Atualiza arquivo TXT para servidor TCP oficial
                            try
                            {
                                await _dataFileService.UpdateProductInFileAsync(produto);
                            }
                            catch (Exception fileEx)
                            {
                                _logger.LogWarning(fileEx, $"Erro ao atualizar produto no arquivo de dados: {produto.Nome}");
                            }
                            
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
                                    
                                    // Envia imagem do produto se disponível (antes de enviar nome e preço)
                                    if (!string.IsNullOrEmpty(produto.Imagem) || !string.IsNullOrEmpty(produto.ImagemPrincipal))
                                    {
                                        try
                                        {
                                            var imagemUrl = produto.ImagemPrincipal ?? produto.Imagem;
                                            if (!string.IsNullOrEmpty(imagemUrl))
                                            {
                                                _logger.LogInformation($"Enviando imagem do produto atualizado: {imagemUrl}");
                                                Console.WriteLine($"Enviando imagem do produto: {produto.Nome}");
                                                var imagemEnviada = await _gertecService.SendImageFromFileAsync(imagemUrl, indice: 0, numeroLoops: 1, tempoExibicao: 5);
                                                if (imagemEnviada)
                                                {
                                                    Console.WriteLine($"Imagem enviada com sucesso para servidor Gertec: {produto.Nome}");
                                                }
                                            }
                                        }
                                        catch (Exception imgEx)
                                        {
                                            _logger.LogWarning(imgEx, $"Erro ao enviar imagem do produto {produto.Nome}. Continuando com nome e preço...");
                                        }
                                    }
                                    
                                    // Envia produto atualizado para o Gertec
                                    bool enviado = await _gertecService.SendProductInfoAsync(nomeFormatado, precoFormatado);
                                    
                                    if (enviado)
                                    {
                                        _logger.LogInformation($"ENVIADO para servidor Gertec com sucesso: {produto.Nome} - Preço: {precoFormatado}");
                                        Console.WriteLine($"ENVIADO para servidor Gertec com sucesso: {produto.Nome} - Preço: {precoFormatado}");
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

                    // Regenera arquivo TXT completo quando há mudanças significativas
                    if (produtosAtualizados > 0 || produtosNovos > 10)
                    {
                        _logger.LogInformation("Regenerando arquivo de dados Gertec devido a mudanças...");
                        Console.WriteLine("Regenerando arquivo de dados Gertec devido a mudanças...");
                        try
                        {
                            await _dataFileService.GenerateDataFileAsync();
                        }
                        catch (Exception fileEx)
                        {
                            _logger.LogError(fileEx, "Erro ao regenerar arquivo de dados Gertec");
                        }
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
                    Console.WriteLine($"Monitoramento concluído: {produtosAtualizados} produtos atualizados, " +
                                     $"{produtosNovos} produtos novos. Próxima verificação em {intervalMinutes} minutos.");
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
        base.Dispose();
    }
}


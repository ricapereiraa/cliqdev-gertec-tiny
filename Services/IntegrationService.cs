using OlistGertecIntegration.Models;
using OlistGertecIntegration.Services;

namespace OlistGertecIntegration.Services;

public class IntegrationService : BackgroundService
{
    private readonly ILogger<IntegrationService> _logger;
    private readonly GertecProtocolService _gertecService;
    private readonly OlistApiService _olistService;
    private readonly GertecDataFileService _dataFileService;
    private readonly GertecProductCacheService _productCacheService;
    private readonly IConfiguration _configuration;
    private DateTime? _lastSyncDate;
    private readonly Dictionary<string, Produto> _productCache = new();

    public IntegrationService(
        ILogger<IntegrationService> logger,
        GertecProtocolService gertecService,
        OlistApiService olistService,
        GertecDataFileService dataFileService,
        GertecProductCacheService productCacheService,
        IConfiguration configuration)
    {
        _logger = logger;
        _gertecService = gertecService;
        _olistService = olistService;
        _dataFileService = dataFileService;
        _productCacheService = productCacheService;
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

        // Carrega produtos do arquivo TXT no cache
        _logger.LogInformation("Carregando produtos do arquivo TXT...");
        Console.WriteLine("Carregando produtos do arquivo TXT...");
        try
        {
            await _productCacheService.LoadProductsFromFileAsync();
            var count = _productCacheService.GetProductCount();
            _logger.LogInformation($"Produtos carregados no cache: {count} produtos");
            Console.WriteLine($"Produtos carregados no cache: {count} produtos");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar produtos do arquivo. Continuando...");
        }

        // Inicia servidor TCP para escutar conexões do Gertec
        _logger.LogInformation("Iniciando servidor TCP para escutar conexões do Gertec...");
        Console.WriteLine("Iniciando servidor TCP para escutar conexões do Gertec...");
        await _gertecService.ConnectAsync();

        // Inicia monitoramento de preços se habilitado
        // Atualiza arquivo TXT e recarrega cache quando detecta mudanças
        var monitoringEnabled = _configuration.GetValue<bool>("PriceMonitoring:Enabled");
        if (monitoringEnabled)
        {
            _logger.LogInformation("Monitoramento de preços habilitado - atualizando arquivo TXT automaticamente");
            Console.WriteLine("Monitoramento de preços habilitado - atualizando arquivo TXT automaticamente");
            _ = Task.Run(() => MonitorPriceChangesAsync(stoppingToken), stoppingToken);
        }

        // Mantém o serviço rodando e recarrega produtos periodicamente
        while (!stoppingToken.IsCancellationRequested)
        {
            // Recarrega produtos do arquivo se foi modificado
            try
            {
                await _productCacheService.RefreshIfNeededAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao recarregar produtos do arquivo");
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
                    // USA APENAS GTIN como chave (nunca código/SKU)
                    var chaveProduto = produto.Gtin;
                    
                    if (string.IsNullOrEmpty(chaveProduto))
                    {
                        // Ignora produtos sem GTIN
                        continue;
                    }

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
                                _logger.LogInformation($"Produto atualizado no arquivo: {produto.Nome} - GTIN: {chaveProduto}");
                                Console.WriteLine($"Produto atualizado no arquivo: {produto.Nome} - GTIN: {chaveProduto}");
                                produtosAtualizados++;
                            }
                            catch (Exception fileEx)
                            {
                                _logger.LogWarning(fileEx, $"Erro ao atualizar produto no arquivo de dados: {produto.Nome}");
                            }
                        }
                    }
                    else
                    {
                        // Novo produto - adiciona ao cache e atualiza arquivo
                        _productCache[chaveProduto] = produto;
                        produtosNovos++;
                        
                        // Atualiza arquivo TXT para novo produto
                        try
                        {
                            await _dataFileService.UpdateProductInFileAsync(produto);
                        }
                        catch (Exception fileEx)
                        {
                            _logger.LogWarning(fileEx, $"Erro ao adicionar novo produto no arquivo: {produto.Nome}");
                        }
                        
                        if (primeiraExecucao)
                        {
                            _logger.LogDebug($"Novo produto adicionado: {produto.Nome} (GTIN: {chaveProduto})");
                        }
                        else
                        {
                            _logger.LogInformation($"Novo produto detectado: {produto.Nome} (GTIN: {chaveProduto})");
                            Console.WriteLine($"Novo produto detectado: {produto.Nome} (GTIN: {chaveProduto})");
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

                    // Regenera arquivo TXT completo quando há mudanças significativas ou na primeira execução
                    if (primeiraExecucao || produtosAtualizados > 0 || produtosNovos > 10)
                    {
                        _logger.LogInformation("Regenerando arquivo de dados Gertec com todos os produtos...");
                        Console.WriteLine("Regenerando arquivo de dados Gertec com todos os produtos...");
                        try
                        {
                            await _dataFileService.GenerateDataFileAsync();
                            
                            // Recarrega produtos do arquivo atualizado
                            await _productCacheService.LoadProductsFromFileAsync();
                            var count = _productCacheService.GetProductCount();
                            _logger.LogInformation($"Produtos recarregados do arquivo: {count} produtos");
                            Console.WriteLine($"Produtos recarregados do arquivo: {count} produtos");
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


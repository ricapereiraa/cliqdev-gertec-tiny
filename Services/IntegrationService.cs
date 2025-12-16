using OlistGertecIntegration.Models;
using OlistGertecIntegration.Services;

namespace OlistGertecIntegration.Services;

public class IntegrationService : BackgroundService
{
    private readonly ILogger<IntegrationService> _logger;
    private readonly OlistApiService _olistService;
    private readonly DatabaseService _databaseService;
    private readonly IConfiguration _configuration;
    private DateTime? _lastSyncDate;
    private readonly Dictionary<string, Produto> _productCache = new();

    public IntegrationService(
        ILogger<IntegrationService> logger,
        OlistApiService olistService,
        DatabaseService databaseService,
        IConfiguration configuration)
    {
        _logger = logger;
        _olistService = olistService;
        _databaseService = databaseService;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de integração iniciado");

        // PASSO 1: Testa conexão com banco de dados
        _logger.LogInformation("Testando conexão com banco de dados...");
        Console.WriteLine("Testando conexão com banco de dados...");
        var conexaoOk = await _databaseService.TestConnectionAsync();
        if (!conexaoOk)
        {
            _logger.LogError("ERRO: Não foi possível conectar ao banco de dados. Verifique as configurações.");
            Console.WriteLine("ERRO: Nao foi possivel conectar ao banco de dados. Verifique as configuracoes.");
            return;
        }

        // PASSO 2: Pré-carrega cache de produtos na inicialização
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

        // PASSO 3: Busca produtos e atualiza no banco de dados
        _logger.LogInformation("Buscando produtos e atualizando no banco de dados...");
        Console.WriteLine("Buscando produtos e atualizando no banco de dados...");
        try
        {
            var produtos = await _olistService.GetAllProductsAsync(null);
            
            if (produtos == null || produtos.Count == 0)
            {
                _logger.LogWarning("Nenhum produto encontrado na API");
                Console.WriteLine("AVISO: Nenhum produto encontrado na API");
            }
            else
            {
                _logger.LogInformation($"Processando {produtos.Count} produtos para atualizar no banco...");
                Console.WriteLine($"Processando {produtos.Count} produtos para atualizar no banco...");
                
                var (inseridos, atualizados, erros) = await _databaseService.UpsertProductsAsync(produtos);
                
                _logger.LogInformation($"Produtos processados no banco: {inseridos} INSERIDOS (novos), {atualizados} ATUALIZADOS (existentes), {erros} erros");
                Console.WriteLine($"Produtos processados no banco: {inseridos} INSERIDOS (novos), {atualizados} ATUALIZADOS (existentes), {erros} erros");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar produtos no banco de dados. Continuando...");
            Console.WriteLine($"ERRO ao atualizar produtos no banco: {ex.Message}");
        }

        // Inicia monitoramento automático - busca atualizações e atualiza banco a cada 1 minuto
        _logger.LogInformation("Monitoramento automático iniciado - atualizando banco a cada 1 minuto");
        Console.WriteLine("Monitoramento automático iniciado - atualizando banco a cada 1 minuto");
        _ = Task.Run(() => MonitorPriceChangesAsync(stoppingToken), stoppingToken);

        // Mantém o serviço rodando
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(60000, stoppingToken); // Aguarda 1 minuto
        }
    }



    private async Task MonitorPriceChangesAsync(CancellationToken cancellationToken)
    {
        // Intervalo fixo de 1 minuto
        var intervalMinutes = 1;
        
        // Sincronização inicial completa na primeira execução
        bool primeiraExecucao = true;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Buscando atualizações no Tiny ERP...");
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
                    // BAR_CODE = GTIN (prioridade) ou Código (fallback)
                    var barCode = produto.Gtin ?? produto.Codigo ?? "";
                    
                    if (string.IsNullOrEmpty(barCode))
                    {
                        // Ignora produtos sem GTIN e sem código
                        continue;
                    }

                    // Verifica se produto existe no banco de dados (não apenas no cache local)
                    var existeNoBanco = await _databaseService.ProductExistsAsync(barCode);
                    
                    // Verifica se está no cache local (para detectar mudanças)
                    var existeNoCache = _productCache.TryGetValue(barCode, out var produtoCache);
                    
                    if (existeNoCache && produtoCache != null)
                    {
                        // Produto existe no cache - verifica se houve mudança
                        bool precoMudou = produtoCache.Preco != produto.Preco || 
                                         produtoCache.PrecoPromocional != produto.PrecoPromocional;
                        bool descricaoMudou = produtoCache.Nome != produto.Nome;
                        
                        if (precoMudou || descricaoMudou)
                        {
                            _logger.LogInformation($"Produto alterado: {produto.Nome} (BAR_CODE: {barCode}) - " +
                                                  $"Preço anterior: {produtoCache.Preco}, Novo preço: {produto.Preco}");
                            
                            // Atualiza cache local
                            _productCache[barCode] = produto;
                            
                            // UPSERT: Se GTIN já existe no banco, ATUALIZA. Se não existe, INSERE.
                            try
                            {
                                await _databaseService.UpsertProductAsync(produto);
                                _logger.LogInformation($"Produto atualizado no banco: {produto.Nome} - BAR_CODE: {barCode}");
                                Console.WriteLine($"Produto atualizado no banco: {produto.Nome} - BAR_CODE: {barCode}");
                                produtosAtualizados++;
                            }
                            catch (Exception dbEx)
                            {
                                _logger.LogWarning(dbEx, $"Erro ao atualizar produto no banco: {produto.Nome}");
                            }
                        }
                        else
                        {
                            // Produto não mudou, apenas atualiza cache local
                            _productCache[barCode] = produto;
                        }
                    }
                    else
                    {
                        // Produto não está no cache local
                        _productCache[barCode] = produto;
                        
                        if (!existeNoBanco)
                        {
                            // NOVO PRODUTO: GTIN não existe no banco - INSERE
                            produtosNovos++;
                            try
                            {
                                await _databaseService.UpsertProductAsync(produto);
                                _logger.LogInformation($"NOVO produto INSERIDO no banco: {produto.Nome} (BAR_CODE: {barCode}, PRICE_1: {produto.Preco}, PRICE_2: {produto.PrecoPromocional})");
                                Console.WriteLine($"NOVO produto INSERIDO: {produto.Nome} - BAR_CODE: {barCode}");
                            }
                            catch (Exception dbEx)
                            {
                                _logger.LogWarning(dbEx, $"Erro ao inserir novo produto no banco: {produto.Nome}");
                            }
                        }
                        else
                        {
                            // Produto existe no banco mas não no cache - atualiza para sincronizar
                            try
                            {
                                await _databaseService.UpsertProductAsync(produto);
                                _logger.LogDebug($"Produto sincronizado: {produto.Nome} (BAR_CODE: {barCode})");
                                produtosAtualizados++;
                            }
                            catch (Exception dbEx)
                            {
                                _logger.LogWarning(dbEx, $"Erro ao sincronizar produto no banco: {produto.Nome}");
                            }
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

                    // Produtos já foram atualizados individualmente no banco durante o loop
                    if (produtosAtualizados > 0 || produtosNovos > 0)
                    {
                        _logger.LogInformation($"Banco atualizado: {produtosNovos} produtos INSERIDOS (novos), {produtosAtualizados} produtos ATUALIZADOS (modificados)");
                        Console.WriteLine($"Banco atualizado: {produtosNovos} produtos INSERIDOS (novos), {produtosAtualizados} produtos ATUALIZADOS (modificados)");
                    }
                }
                
                if (primeiraExecucao)
                {
                    _logger.LogInformation($"Sincronização inicial concluída: {produtos.Count} produtos no banco. " +
                                          $"Próxima verificação em {intervalMinutes} minuto(s).");
                    Console.WriteLine($"Sincronização inicial concluída: {produtos.Count} produtos. Próxima verificação em {intervalMinutes} minuto(s).");
                    primeiraExecucao = false;
                }
                else
                {
                    _logger.LogInformation($"Atualização concluída: {produtosNovos} produtos INSERIDOS (novos), " +
                                          $"{produtosAtualizados} produtos ATUALIZADOS (modificados). Banco atualizado. Próxima verificação em {intervalMinutes} minuto(s).");
                    Console.WriteLine($"Atualização concluída: {produtosNovos} produtos INSERIDOS (novos), " +
                                     $"{produtosAtualizados} produtos ATUALIZADOS (modificados). Próxima verificação em {intervalMinutes} minuto(s).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no monitoramento de preços");
            }

            // Aguarda 1 minuto antes da próxima verificação
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), cancellationToken);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}


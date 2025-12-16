using OlistGertecIntegration.Models;
using OlistGertecIntegration.Services;

namespace OlistGertecIntegration.Services;

public class IntegrationService : BackgroundService
{
    private readonly ILogger<IntegrationService> _logger;
    private readonly OlistApiService _olistService;
    private readonly GertecDataFileService _dataFileService;
    private readonly IConfiguration _configuration;
    private DateTime? _lastSyncDate;
    private readonly Dictionary<string, Produto> _productCache = new();

    public IntegrationService(
        ILogger<IntegrationService> logger,
        OlistApiService olistService,
        GertecDataFileService dataFileService,
        IConfiguration configuration)
    {
        _logger = logger;
        _olistService = olistService;
        _dataFileService = dataFileService;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de integração iniciado");

        // PASSO 1: Garante que arquivo existe ANTES de qualquer operação
        _logger.LogInformation("Garantindo que arquivo existe ANTES de buscar produtos...");
        Console.WriteLine("Garantindo que arquivo existe ANTES de buscar produtos...");
        if (!_dataFileService.FileExists())
        {
            _logger.LogInformation("Criando arquivo vazio ANTES de buscar produtos...");
            Console.WriteLine("Criando arquivo vazio ANTES de buscar produtos...");
            await _dataFileService.CreateEmptyFileAsync();
        }
        else
        {
            _logger.LogInformation("Arquivo já existe, será atualizado com produtos encontrados");
            Console.WriteLine("Arquivo já existe, será atualizado com produtos encontrados");
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

        // PASSO 3: Busca produtos e registra no arquivo (arquivo já existe, será atualizado)
        _logger.LogInformation("Buscando produtos e registrando no arquivo...");
        Console.WriteLine("Buscando produtos e registrando no arquivo...");
        try
        {
            var sucesso = await _dataFileService.GenerateDataFileAsync();
            if (sucesso)
            {
                var caminho = _dataFileService.GetDataFilePath();
                var existe = _dataFileService.FileExists();
                _logger.LogInformation($"Produtos registrados no arquivo. Caminho: {caminho}, Existe: {existe}");
                Console.WriteLine($"Produtos registrados no arquivo. Caminho: {caminho}, Existe: {existe}");
            }
            else
            {
                _logger.LogWarning("Falha ao registrar produtos no arquivo");
                Console.WriteLine("AVISO: Falha ao registrar produtos no arquivo");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar produtos no arquivo. Continuando...");
            Console.WriteLine($"ERRO ao registrar produtos no arquivo: {ex.Message}");
        }

        // Inicia monitoramento automático - busca atualizações e atualiza arquivo a cada 1 minuto
        _logger.LogInformation("Monitoramento automático iniciado - atualizando arquivo a cada 1 minuto");
        Console.WriteLine("Monitoramento automático iniciado - atualizando arquivo a cada 1 minuto");
        _ = Task.Run(() => MonitorPriceChangesAsync(stoppingToken), stoppingToken);

        // Mantém o serviço rodando
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(60000, stoppingToken); // Aguarda 1 minuto
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

                    // Apenas atualiza produtos individuais no arquivo existente (não regenera tudo)
                    // O arquivo já foi criado na inicialização, apenas atualizamos produtos modificados
                    if (produtosAtualizados > 0 || produtosNovos > 0)
                    {
                        _logger.LogInformation($"Arquivo atualizado: {produtosAtualizados} produtos modificados, {produtosNovos} produtos novos adicionados");
                        Console.WriteLine($"Arquivo atualizado: {produtosAtualizados} produtos modificados, {produtosNovos} produtos novos adicionados");
                    }
                }
                
                if (primeiraExecucao)
                {
                    _logger.LogInformation($"Sincronização inicial concluída: {produtos.Count} produtos no arquivo. " +
                                          $"Próxima verificação em {intervalMinutes} minuto(s).");
                    Console.WriteLine($"Sincronização inicial concluída: {produtos.Count} produtos. Próxima verificação em {intervalMinutes} minuto(s).");
                    primeiraExecucao = false;
                }
                else
                {
                    _logger.LogInformation($"Atualização concluída: {produtosAtualizados} produtos atualizados, " +
                                          $"{produtosNovos} produtos novos. Arquivo atualizado. Próxima verificação em {intervalMinutes} minuto(s).");
                    Console.WriteLine($"Atualização concluída: {produtosAtualizados} produtos atualizados, " +
                                     $"{produtosNovos} produtos novos. Próxima verificação em {intervalMinutes} minuto(s).");
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


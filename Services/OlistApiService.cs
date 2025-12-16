using System.Text;
using System.Linq;
using Newtonsoft.Json;
using OlistGertecIntegration.Models;

namespace OlistGertecIntegration.Services;

public class OlistApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OlistApiService> _logger;
    private string _token; // Não readonly para permitir atualização em tempo de execução
    private readonly string _baseUrl;
    private readonly string _format;

    // Cache completo de produtos indexado por GTIN e código
    // Estratégia: Pré-carregar todos os produtos na inicialização e atualizar quando houver mudanças
    private static readonly Dictionary<string, Produto> _gtinCache = new(); // Chave: GTIN
    private static readonly Dictionary<string, Produto> _codigoCache = new(); // Chave: Código (SKU)
    private static DateTime _lastFullSync = DateTime.MinValue;
    private static readonly object _cacheLock = new object();
    private static bool _cachePreCarregado = false;

    public OlistApiService(HttpClient httpClient, ILogger<OlistApiService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Prioridade: Variável de ambiente > Configuration
        // Formato no .env: OLIST_API__TOKEN, OLIST_API__BASE_URL, OLIST_API__FORMAT
        _token = Environment.GetEnvironmentVariable("OLIST_API__TOKEN") 
            ?? configuration["OlistApi:Token"] 
            ?? throw new InvalidOperationException("Token do Olist não configurado. Configure OLIST_API__TOKEN no .env");
        
        _baseUrl = Environment.GetEnvironmentVariable("OLIST_API__BASE_URL") 
            ?? configuration["OlistApi:BaseUrl"] 
            ?? throw new InvalidOperationException("BaseUrl do Olist não configurado. Configure OLIST_API__BASE_URL no .env");
        
        _format = Environment.GetEnvironmentVariable("OLIST_API__FORMAT") 
            ?? configuration["OlistApi:Format"] 
            ?? "json";
        
        _logger.LogInformation($"OlistApiService inicializado - BaseUrl: {_baseUrl}, Token: {(_token.Length > 8 ? _token.Substring(0, 4) + "..." : "CONFIGURADO")}");
    }

    /// <summary>
    /// Atualiza o token em tempo de execução (quando alterado via painel)
    /// </summary>
    public void UpdateToken(string newToken)
    {
        _token = newToken;
        _logger.LogInformation("Token da API Olist atualizado em tempo de execução");
    }

    /// <summary>
    /// Pré-carrega todos os produtos no cache (chamado na inicialização)
    /// </summary>
    public async Task PreloadCacheAsync()
    {
        if (_cachePreCarregado)
        {
            _logger.LogDebug("Cache já foi pré-carregado anteriormente");
            return;
        }

        try
        {
            _logger.LogInformation("Iniciando pré-carregamento do cache de produtos...");
            
            var todosProdutos = await GetAllProductsAsync();
            
            lock (_cacheLock)
            {
                _gtinCache.Clear();
                _codigoCache.Clear();
                
                foreach (var produto in todosProdutos)
                {
                    // Indexa por GTIN
                    if (!string.IsNullOrEmpty(produto.Gtin))
                    {
                        _gtinCache[produto.Gtin] = produto;
                    }
                    
                    // Indexa por código
                    if (!string.IsNullOrEmpty(produto.Codigo))
                    {
                        _codigoCache[produto.Codigo] = produto;
                    }
                }
                
                _lastFullSync = DateTime.Now;
                _cachePreCarregado = true;
            }
            
            _logger.LogInformation($"Cache pré-carregado com sucesso: {_gtinCache.Count} produtos por GTIN, {_codigoCache.Count} produtos por código");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao pré-carregar cache de produtos");
        }
    }

    /// <summary>
    /// Atualiza o cache completo (chamado quando detecta mudanças de preço ou novos produtos)
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        try
        {
            _logger.LogInformation("Atualizando cache completo de produtos...");
            
            var todosProdutos = await GetAllProductsAsync();
            
            lock (_cacheLock)
            {
                _gtinCache.Clear();
                _codigoCache.Clear();
                
                foreach (var produto in todosProdutos)
                {
                    // Indexa por GTIN
                    if (!string.IsNullOrEmpty(produto.Gtin))
                    {
                        _gtinCache[produto.Gtin] = produto;
                    }
                    
                    // Indexa por código
                    if (!string.IsNullOrEmpty(produto.Codigo))
                    {
                        _codigoCache[produto.Codigo] = produto;
                    }
                }
                
                _lastFullSync = DateTime.Now;
            }
            
            _logger.LogInformation($"Cache atualizado: {_gtinCache.Count} produtos por GTIN, {_codigoCache.Count} produtos por código");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar cache de produtos");
        }
    }

    public async Task<Produto?> GetProductByBarcodeAsync(string barcode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return null;
            }

            // PRIMEIRA TENTATIVA: Busca direto no cache pré-carregado (MUITO RÁPIDO! )
            // Como o cache já foi pré-carregado na inicialização, a maioria dos produtos estará aqui
            Produto? produtoCache = null;
            lock (_cacheLock)
            {
                // Tenta buscar por GTIN primeiro
                if (_gtinCache.TryGetValue(barcode, out var produtoGtin))
                {
                    produtoCache = produtoGtin;
                    _logger.LogInformation($"Produto encontrado no cache por GTIN (instantâneo): {produtoCache.Nome}");
                }
                // Se não encontrou por GTIN, tenta por código
                else if (_codigoCache.TryGetValue(barcode, out var produtoCodigo))
                {
                    produtoCache = produtoCodigo;
                    _logger.LogInformation($"Produto encontrado no cache por código (instantâneo): {produtoCache.Nome}");
                }
            }
            
            // Se encontrou no cache, busca imagem se necessário e retorna
            if (produtoCache != null)
            {
                // Se precisa de imagem e ainda não tem, busca via produto.obter.php (assíncrono, não bloqueia)
                if (produtoCache.Id > 0 && string.IsNullOrEmpty(produtoCache.Imagem))
                {
                    try
                    {
                        var produtoComImagem = await GetProductByIdAsync(produtoCache.Id);
                        if (produtoComImagem != null && !string.IsNullOrEmpty(produtoComImagem.Imagem))
                        {
                            produtoCache.Imagem = produtoComImagem.Imagem;
                            produtoCache.ImagemPrincipal = produtoComImagem.ImagemPrincipal;
                            
                            // Atualiza no cache
                            lock (_cacheLock)
                            {
                                if (!string.IsNullOrEmpty(produtoCache.Gtin))
                                    _gtinCache[produtoCache.Gtin] = produtoCache;
                                if (!string.IsNullOrEmpty(produtoCache.Codigo))
                                    _codigoCache[produtoCache.Codigo] = produtoCache;
                            }
                            
                            _logger.LogDebug($"Imagem obtida para produto {produtoCache.Nome}: {produtoCache.Imagem}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Erro ao buscar imagem do produto {produtoCache.Id}, continuando sem imagem...");
                    }
                }
                
                return produtoCache;
            }
            
            // SEGUNDA TENTATIVA: Se não encontrou no cache, pode ser produto novo - busca na API
            _logger.LogWarning($"Produto {barcode} não encontrado no cache. Buscando na API (produto novo?)...");
            
            // Tenta busca direta na API primeiro (mais rápida que buscar todos)
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", _token),
                new KeyValuePair<string, string>("formato", _format),
                new KeyValuePair<string, string>("pesquisa", barcode)
            });

            var response = await _httpClient.PostAsync($"{_baseUrl}/produtos.pesquisa.php", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Log detalhado apenas em desenvolvimento
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Resposta da API Olist (busca direta): {responseContent}");
            }

            // Estrutura baseada na documentação oficial: https://tiny.com.br/api-docs/api2-produtos-pesquisar
            var apiResponse = JsonConvert.DeserializeObject<TinyApiResponse>(responseContent);
            
            if (apiResponse?.Retorno?.Status == "OK" && 
                apiResponse.Retorno.Produtos != null && 
                apiResponse.Retorno.Produtos.Count > 0)
            {
                // Procura no array por código ou GTIN correspondente
                var produtoEncontrado = apiResponse.Retorno.Produtos
                    .FirstOrDefault(p => p.Produto != null && 
                        (p.Produto.Codigo == barcode || p.Produto.Gtin == barcode));
                
                if (produtoEncontrado?.Produto != null)
                {
                    var produtoNovo = produtoEncontrado.Produto;
                    
                    // Produto novo encontrado - adiciona ao cache
                    lock (_cacheLock)
                    {
                        if (!string.IsNullOrEmpty(produtoNovo.Gtin))
                            _gtinCache[produtoNovo.Gtin] = produtoNovo;
                        if (!string.IsNullOrEmpty(produtoNovo.Codigo))
                            _codigoCache[produtoNovo.Codigo] = produtoNovo;
                    }
                    
                    _logger.LogInformation($"Produto novo encontrado na API e adicionado ao cache: {produtoNovo.Nome}");
                    return produtoNovo;
                }
            }
            
            // TERCEIRA TENTATIVA: Se busca direta não funcionou, busca todos e filtra (fallback)
            var todosProdutos = await GetAllProductsAsync();
            var produtoPorGtin = todosProdutos
                .FirstOrDefault(p => (!string.IsNullOrEmpty(p.Gtin) && p.Gtin == barcode) ||
                                     (!string.IsNullOrEmpty(p.Codigo) && p.Codigo == barcode));
            
            if (produtoPorGtin != null)
            {
                // Produto encontrado - adiciona ao cache
                lock (_cacheLock)
                {
                    if (!string.IsNullOrEmpty(produtoPorGtin.Gtin))
                        _gtinCache[produtoPorGtin.Gtin] = produtoPorGtin;
                    if (!string.IsNullOrEmpty(produtoPorGtin.Codigo))
                        _codigoCache[produtoPorGtin.Codigo] = produtoPorGtin;
                        }
                
                _logger.LogInformation($"Produto encontrado em busca completa e adicionado ao cache: {produtoPorGtin.Nome}");
                return produtoPorGtin;
            }
            
            // Se houver erro na primeira busca, loga
            if (apiResponse?.Retorno?.Status == "Erro")
            {
                var erros = apiResponse.Retorno.Erros?.Select(e => e.Erro).ToList() ?? new List<string>();
                _logger.LogWarning($"Erro na API Olist: {string.Join(", ", erros)}");
            }

            _logger.LogWarning($"Produto não encontrado para código de barras: {barcode}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao buscar produto por código de barras: {barcode}");
            return null;
        }
    }

    public async Task<List<Produto>> GetAllProductsAsync(DateTime? sinceDate = null)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", _token),
                new KeyValuePair<string, string>("formato", _format)
            });

            var response = await _httpClient.PostAsync($"{_baseUrl}/produtos.pesquisa.php", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Log detalhado apenas em desenvolvimento
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Resposta da API Olist (todos produtos): {responseContent}");
            }

            // Estrutura baseada na documentação oficial
            var apiResponse = JsonConvert.DeserializeObject<TinyApiResponse>(responseContent);
            
            if (apiResponse?.Retorno?.Status == "OK" && apiResponse.Retorno.Produtos != null)
            {
                // Extrai a lista de produtos do wrapper
                return apiResponse.Retorno.Produtos
                    .Where(p => p.Produto != null)
                    .Select(p => p.Produto!)
                    .ToList();
            }
            
            // Se houver erro, loga
            if (apiResponse?.Retorno?.Status == "Erro")
            {
                var erros = apiResponse.Retorno.Erros?.Select(e => e.Erro).ToList() ?? new List<string>();
                _logger.LogWarning($"Erro na API Olist: {string.Join(", ", erros)}");
            }

            return new List<Produto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar todos os produtos");
            return new List<Produto>();
        }
    }

    /// <summary>
    /// Busca o ID do produto pelo GTIN (otimizado - busca apenas IDs, não todos os dados)
    /// </summary>
    private async Task<int?> GetProductIdByGtinAsync(string gtin)
    {
        try
        {
            // Busca todos os produtos (mas só precisamos do ID e GTIN)
            var todosProdutos = await GetAllProductsAsync();
            
            // Filtra pelo GTIN e retorna apenas o ID
            var produto = todosProdutos
                .FirstOrDefault(p => !string.IsNullOrEmpty(p.Gtin) && p.Gtin == gtin);
            
            if (produto != null)
            {
                _logger.LogDebug($"ID encontrado para GTIN {gtin}: {produto.Id}");
                return produto.Id;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao buscar ID do produto pelo GTIN: {gtin}");
            return null;
        }
    }

    /// <summary>
    /// Obtém produto completo usando produto.obter.php (retorna dados completos incluindo imagens)
    /// </summary>
    private async Task<Produto?> GetProductByIdAsync(int productId)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", _token),
                new KeyValuePair<string, string>("formato", _format),
                new KeyValuePair<string, string>("id", productId.ToString())
            });

            var response = await _httpClient.PostAsync($"{_baseUrl}/produto.obter.php", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Resposta da API produto.obter.php: {responseContent}");
            }

            // Estrutura de resposta do produto.obter.php
            var apiResponse = JsonConvert.DeserializeObject<TinyApiResponse>(responseContent);
            
            if (apiResponse?.Retorno?.Status == "OK" && apiResponse.Retorno.Produto != null)
            {
                var produto = apiResponse.Retorno.Produto;
                
                // Se houver anexos (imagens), pega o primeiro
                if (apiResponse.Retorno.ProdutoAnexos != null && 
                    apiResponse.Retorno.ProdutoAnexos.Count > 0)
                {
                    produto.Imagem = apiResponse.Retorno.ProdutoAnexos[0].Anexo;
                    produto.ImagemPrincipal = apiResponse.Retorno.ProdutoAnexos[0].Anexo;
                    _logger.LogDebug($"Imagem do produto encontrada: {produto.Imagem}");
                }
                
                _logger.LogInformation($"Produto completo obtido via produto.obter.php: {produto.Nome} (ID: {productId})");
                return produto;
            }
            
            if (apiResponse?.Retorno?.Status == "Erro")
            {
                var erros = apiResponse.Retorno.Erros?.Select(e => e.Erro).ToList() ?? new List<string>();
                _logger.LogWarning($"Erro ao obter produto por ID {productId}: {string.Join(", ", erros)}");
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter produto por ID: {productId}");
            return null;
        }
    }

    public string FormatPrice(string price)
    {
        if (decimal.TryParse(price, out decimal valor))
        {
            return $"R$ {valor:F2}";
        }
        return price;
    }

    /// <summary>
    /// Invalida o cache de um produto específico (não usado mais - cache é atualizado automaticamente)
    /// Mantido para compatibilidade
    /// </summary>
    public void InvalidateCache(string? codigo, string? gtin)
    {
        // Não precisa mais invalidar - o cache é atualizado automaticamente via RefreshCacheAsync()
        _logger.LogDebug($"InvalidateCache chamado para código: {codigo}, GTIN: {gtin} (cache será atualizado automaticamente)");
    }

    /// <summary>
    /// Limpa todo o cache (útil para resetar)
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _gtinCache.Clear();
            _codigoCache.Clear();
            _lastFullSync = DateTime.MinValue;
            _cachePreCarregado = false;
            _logger.LogInformation("Cache de produtos limpo completamente");
        }
    }

    /// <summary>
    /// Obtém estatísticas do cache
    /// </summary>
    public CacheStats GetCacheStats()
    {
        lock (_cacheLock)
        {
            // Conta produtos únicos (pode haver duplicatas entre GTIN e código)
            var produtosUnicos = new HashSet<int>();
            foreach (var produto in _gtinCache.Values)
            {
                produtosUnicos.Add(produto.Id);
            }
            foreach (var produto in _codigoCache.Values)
            {
                produtosUnicos.Add(produto.Id);
            }

            return new CacheStats
            {
                TotalProdutos = produtosUnicos.Count,
                ProdutosPorGtin = _gtinCache.Count,
                ProdutosPorCodigo = _codigoCache.Count,
                UltimaAtualizacao = _lastFullSync,
                CachePreCarregado = _cachePreCarregado
            };
        }
    }
}

public class CacheStats
{
    public int TotalProdutos { get; set; }
    public int ProdutosPorGtin { get; set; }
    public int ProdutosPorCodigo { get; set; }
    public DateTime UltimaAtualizacao { get; set; }
    public bool CachePreCarregado { get; set; }
}


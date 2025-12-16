using System.Text;
using OlistGertecIntegration.Models;

namespace OlistGertecIntegration.Services;

/// <summary>
/// Serviço para carregar e indexar produtos do arquivo TXT em memória
/// </summary>
public class GertecProductCacheService
{
    private readonly ILogger<GertecProductCacheService> _logger;
    private readonly GertecDataFileService _dataFileService;
    private readonly Dictionary<string, ProdutoArquivo> _productCache = new();
    private readonly object _cacheLock = new object();
    private DateTime _lastFileUpdate = DateTime.MinValue;

    public GertecProductCacheService(
        ILogger<GertecProductCacheService> logger,
        GertecDataFileService dataFileService)
    {
        _logger = logger;
        _dataFileService = dataFileService;
    }

    /// <summary>
    /// Carrega produtos do arquivo TXT e indexa por GTIN
    /// </summary>
    public async Task<bool> LoadProductsFromFileAsync()
    {
        try
        {
            var filePath = _dataFileService.GetDataFilePath();
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Arquivo de produtos não encontrado: {filePath}");
                Console.WriteLine($"Arquivo de produtos não encontrado: {filePath}");
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.LastWriteTime <= _lastFileUpdate)
            {
                // Arquivo não foi modificado desde última leitura
                return true;
            }

            _logger.LogInformation($"Carregando produtos do arquivo: {filePath}");
            Console.WriteLine($"Carregando produtos do arquivo: {filePath}");

            var linhas = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
            
            lock (_cacheLock)
            {
                _productCache.Clear();
                int produtosCarregados = 0;
                int produtosComErro = 0;

                foreach (var linha in linhas)
                {
                    if (string.IsNullOrWhiteSpace(linha))
                        continue;

                    try
                    {
                        // Formato: GTIN|NOME|PRECO|IMAGEM
                        var partes = linha.Split('|');
                        if (partes.Length >= 3)
                        {
                            var gtin = partes[0].Trim();
                            var nome = partes[1].Trim();
                            var preco = partes[2].Trim();
                            var imagem = partes.Length > 3 ? partes[3].Trim() : "";

                            if (!string.IsNullOrEmpty(gtin))
                            {
                                _productCache[gtin] = new ProdutoArquivo
                                {
                                    Gtin = gtin,
                                    Nome = nome,
                                    Preco = preco,
                                    Imagem = imagem
                                };
                                produtosCarregados++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        produtosComErro++;
                        _logger.LogWarning(ex, $"Erro ao processar linha do arquivo: {linha.Substring(0, Math.Min(50, linha.Length))}");
                    }
                }

                _lastFileUpdate = fileInfo.LastWriteTime;
                _logger.LogInformation($"Produtos carregados do arquivo: {produtosCarregados} produtos, {produtosComErro} erros");
                Console.WriteLine($"Produtos carregados do arquivo: {produtosCarregados} produtos");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar produtos do arquivo");
            Console.WriteLine($"Erro ao carregar produtos do arquivo: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Busca produto por GTIN (código de barras)
    /// </summary>
    public ProdutoArquivo? GetProductByGtin(string gtin)
    {
        lock (_cacheLock)
        {
            _productCache.TryGetValue(gtin, out var produto);
            return produto;
        }
    }

    /// <summary>
    /// Recarrega produtos do arquivo se foi modificado
    /// </summary>
    public async Task RefreshIfNeededAsync()
    {
        var filePath = _dataFileService.GetDataFilePath();
        if (File.Exists(filePath))
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.LastWriteTime > _lastFileUpdate)
            {
                await LoadProductsFromFileAsync();
            }
        }
    }

    /// <summary>
    /// Retorna quantidade de produtos no cache
    /// </summary>
    public int GetProductCount()
    {
        lock (_cacheLock)
        {
            return _productCache.Count;
        }
    }
}

/// <summary>
/// Representa um produto lido do arquivo TXT
/// </summary>
public class ProdutoArquivo
{
    public string Gtin { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Preco { get; set; } = string.Empty;
    public string Imagem { get; set; } = string.Empty;
}


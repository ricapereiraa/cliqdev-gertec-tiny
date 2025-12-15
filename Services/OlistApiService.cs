using System.Text;
using System.Linq;
using Newtonsoft.Json;
using OlistGertecIntegration.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;

namespace OlistGertecIntegration.Services;

public class OlistApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OlistApiService> _logger;
    private readonly string _token;
    private readonly string _baseUrl;
    private readonly string _format;

    public OlistApiService(HttpClient httpClient, ILogger<OlistApiService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        var olistConfig = configuration.GetSection("OlistApi");
        _token = olistConfig["Token"] ?? throw new InvalidOperationException("Token do Olist não configurado");
        _baseUrl = olistConfig["BaseUrl"] ?? throw new InvalidOperationException("BaseUrl do Olist não configurado");
        _format = olistConfig["Format"] ?? "json";
    }

    public async Task<Produto?> GetProductByBarcodeAsync(string barcode)
    {
        try
        {
            var requestData = new
            {
                token = _token,
                formato = _format,
                pesquisa = barcode
            };

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
                _logger.LogDebug($"Resposta da API Olist: {responseContent}");
            }

            // Estrutura baseada na documentação oficial: https://tiny.com.br/api-docs/api2-produtos-pesquisar
            var apiResponse = JsonConvert.DeserializeObject<TinyApiResponse>(responseContent);
            
            if (apiResponse?.Retorno?.Status == "OK" && 
                apiResponse.Retorno.Produtos != null && 
                apiResponse.Retorno.Produtos.Count > 0)
            {
                // Retorna o primeiro produto encontrado (busca por código de barras geralmente retorna 1 resultado)
                var produto = apiResponse.Retorno.Produtos[0].Produto;
                
                // Se o código de barras pesquisado não corresponde ao código do produto retornado,
                // tenta encontrar o produto correto no array
                if (produto != null && !string.IsNullOrEmpty(barcode))
                {
                    // Verifica se o código do produto corresponde ao código de barras pesquisado
                    if (produto.Codigo != barcode && produto.Gtin != barcode)
                    {
                        // Procura no array por código ou GTIN correspondente
                        var produtoCorreto = apiResponse.Retorno.Produtos
                            .FirstOrDefault(p => p.Produto?.Codigo == barcode || p.Produto?.Gtin == barcode);
                        
                        if (produtoCorreto?.Produto != null)
                        {
                            return produtoCorreto.Produto;
                        }
                    }
                }
                
                return produto;
            }
            
            // Se houver erro, loga
            if (apiResponse?.Retorno?.Status == "Erro")
            {
                var erros = apiResponse.Retorno.Erros?.Select(e => e.Erro).ToList() ?? new List<string>();
                _logger.LogWarning($"Erro na API Olist: {string.Join(", ", erros)}");
            }

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

    public string FormatPrice(string price)
    {
        if (decimal.TryParse(price, out decimal valor))
        {
            return $"R$ {valor:F2}";
        }
        return price;
    }

    public async Task<byte[]?> DownloadAndConvertImageAsync(string imageUrl)
    {
        try
        {
            // Baixa a imagem
            var imageResponse = await _httpClient.GetAsync(imageUrl);
            if (!imageResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Erro ao baixar imagem: {imageUrl} - Status: {imageResponse.StatusCode}");
                return null;
            }

            var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
            
            // Processa a imagem: redimensiona para 320x240 e converte para GIF
            using (var image = await Image.LoadAsync(imageBytes))
            {
                // Redimensiona mantendo proporção
                var resizeOptions = new ResizeOptions
                {
                    Size = new Size(320, 240),
                    Mode = ResizeMode.Max
                };
                
                image.Mutate(x => x.Resize(resizeOptions));

                // Cria um novo canvas 320x240 com fundo branco
                using (var outputImage = new Image<Rgba32>(320, 240))
                {
                    outputImage.Mutate(ctx =>
                    {
                        ctx.BackgroundColor(Color.White);
                        // Centraliza a imagem redimensionada
                        var x = (320 - image.Width) / 2;
                        var y = (240 - image.Height) / 2;
                        ctx.DrawImage(image, new Point(x, y), 1f);
                    });

                    // Converte para GIF
                    using (var ms = new MemoryStream())
                    {
                        await outputImage.SaveAsync(ms, new GifEncoder());
                        var gifBytes = ms.ToArray();
                        
                        // Valida tamanho máximo (124KB para G2 S)
                        const int maxSize = 124 * 1024;
                        if (gifBytes.Length > maxSize)
                        {
                            _logger.LogWarning($"Imagem convertida muito grande ({gifBytes.Length} bytes). Tentando reduzir qualidade...");
                            
                            // Tenta reduzir qualidade redimensionando novamente
                            using (var compressedImage = new Image<Rgba32>(320, 240))
                            {
                                // Redimensiona imagem original para tamanho menor primeiro
                                using (var tempImage = await Image.LoadAsync(imageBytes))
                                {
                                    tempImage.Mutate(x => x.Resize(new ResizeOptions
                                    {
                                        Size = new Size(280, 210), // Tamanho menor
                                        Mode = ResizeMode.Max
                                    }));
                                    
                                    compressedImage.Mutate(ctx =>
                                    {
                                        ctx.BackgroundColor(Color.White);
                                        var x = (320 - tempImage.Width) / 2;
                                        var y = (240 - tempImage.Height) / 2;
                                        ctx.DrawImage(tempImage, new Point(x, y), 1f);
                                    });
                                }
                                
                                using (var ms2 = new MemoryStream())
                                {
                                    await compressedImage.SaveAsync(ms2, new GifEncoder());
                                    gifBytes = ms2.ToArray();
                                    
                                    if (gifBytes.Length > maxSize)
                                    {
                                        _logger.LogError($"Imagem ainda muito grande após compressão: {gifBytes.Length} bytes. Limite: {maxSize} bytes");
                                        return null;
                                    }
                                }
                            }
                        }
                        
                        _logger.LogInformation($"Imagem convertida para GIF: {gifBytes.Length} bytes (320x240)");
                        return gifBytes;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao baixar e converter imagem: {imageUrl}");
            return null;
        }
    }
}


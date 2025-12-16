using System.Text;
using OlistGertecIntegration.Models;

namespace OlistGertecIntegration.Services;

/// <summary>
/// Serviço para gerar e atualizar arquivo TXT com dados dos produtos para o servidor TCP oficial do Gertec
/// </summary>
public class GertecDataFileService
{
    private readonly ILogger<GertecDataFileService> _logger;
    private readonly IConfiguration _configuration;
    private readonly OlistApiService _olistService;
    private string _dataFilePath;

    public GertecDataFileService(
        ILogger<GertecDataFileService> logger,
        IConfiguration configuration,
        OlistApiService olistService)
    {
        _logger = logger;
        _configuration = configuration;
        _olistService = olistService;

        // Obtém o caminho do arquivo de configuração ou usa padrão
        var customPath = Environment.GetEnvironmentVariable("GERTEC__DATA_FILE_PATH");
        if (string.IsNullOrEmpty(customPath))
        {
            // Caminho padrão: na raiz do projeto ou diretório atual
            _dataFilePath = Path.Combine(Directory.GetCurrentDirectory(), "gertec_produtos.txt");
        }
        else
        {
            _dataFilePath = customPath;
        }

        _logger.LogInformation($"Arquivo de dados Gertec será salvo em: {_dataFilePath}");
        Console.WriteLine($"Arquivo de dados Gertec será salvo em: {_dataFilePath}");
    }

    /// <summary>
    /// Gera ou atualiza o arquivo TXT com todos os produtos do Tiny ERP
    /// Formato: GTIN|NOME|PRECO|IMAGEM
    /// IMPORTANTE: Usa apenas GTIN (código de barras), nunca SKU/código interno
    /// </summary>
    public async Task<bool> GenerateDataFileAsync()
    {
        try
        {
            _logger.LogInformation("Gerando arquivo de dados para servidor TCP Gertec...");
            Console.WriteLine("Gerando arquivo de dados para servidor TCP Gertec...");

            // Busca todos os produtos do Tiny ERP
            var produtos = await _olistService.GetAllProductsAsync(null);

            if (produtos == null || produtos.Count == 0)
            {
                _logger.LogWarning("Nenhum produto encontrado para gerar arquivo de dados");
                Console.WriteLine("Nenhum produto encontrado para gerar arquivo de dados");
                return false;
            }

            var linhas = new List<string>();
            int produtosProcessados = 0;
            int produtosComErro = 0;

            foreach (var produto in produtos)
            {
                try
                {
                    // USA APENAS GTIN (nunca código/SKU)
                    var gtin = produto.Gtin;

                    if (string.IsNullOrEmpty(gtin))
                    {
                        produtosComErro++;
                        _logger.LogDebug($"Produto {produto.Nome} ignorado: sem GTIN (código de barras)");
                        continue;
                    }

                    // Formata nome (remove quebras de linha e caracteres especiais)
                    var nome = produto.Nome
                        .Replace("\n", " ")
                        .Replace("\r", " ")
                        .Replace("|", " ")
                        .Trim();

                    // Usa preço promocional se disponível e maior que zero, senão usa preço normal
                    var preco = !string.IsNullOrEmpty(produto.PrecoPromocional) && 
                               decimal.TryParse(produto.PrecoPromocional, out var precoPromo) && 
                               precoPromo > 0
                        ? produto.PrecoPromocional 
                        : produto.Preco;

                    // Formata preço (remove caracteres especiais, mantém apenas números e ponto/vírgula)
                    var precoFormatado = preco
                        .Replace("R$", "")
                        .Replace("$", "")
                        .Replace(" ", "")
                        .Trim();

                    // Caminho da imagem (URL ou caminho local)
                    var imagem = produto.ImagemPrincipal ?? produto.Imagem ?? "";

                    // Formato: GTIN|NOME|PRECO|IMAGEM (GTIN = código de barras, nunca SKU)
                    var linha = $"{gtin}|{nome}|{precoFormatado}|{imagem}";
                    linhas.Add(linha);
                    produtosProcessados++;
                }
                catch (Exception ex)
                {
                    produtosComErro++;
                    _logger.LogWarning(ex, $"Erro ao processar produto {produto.Nome} para arquivo de dados");
                }
            }

            // Cria diretório se não existir
            var directory = Path.GetDirectoryName(_dataFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Escreve arquivo
            await File.WriteAllLinesAsync(_dataFilePath, linhas, Encoding.UTF8);

            _logger.LogInformation($"Arquivo de dados Gertec gerado com sucesso: {produtosProcessados} produtos, {produtosComErro} erros. Caminho: {_dataFilePath}");
            Console.WriteLine($"Arquivo de dados Gertec gerado com sucesso: {produtosProcessados} produtos processados");
            Console.WriteLine($"Arquivo salvo em: {_dataFilePath}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar arquivo de dados Gertec");
            Console.WriteLine($"Erro ao gerar arquivo de dados Gertec: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Atualiza apenas um produto específico no arquivo
    /// </summary>
    public async Task<bool> UpdateProductInFileAsync(Produto produto)
    {
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                // Se arquivo não existe, gera completo
                return await GenerateDataFileAsync();
            }

            var linhas = (await File.ReadAllLinesAsync(_dataFilePath)).ToList();
            
            // USA APENAS GTIN (nunca código/SKU)
            var gtin = produto.Gtin;

            if (string.IsNullOrEmpty(gtin))
            {
                _logger.LogDebug($"Produto {produto.Nome} ignorado: sem GTIN (código de barras)");
                return false;
            }

            // Formata dados do produto
            var nome = produto.Nome
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("|", " ")
                .Trim();

            var preco = !string.IsNullOrEmpty(produto.PrecoPromocional) && 
                       decimal.TryParse(produto.PrecoPromocional, out var precoPromo) && 
                       precoPromo > 0
                ? produto.PrecoPromocional 
                : produto.Preco;

            var precoFormatado = preco
                .Replace("R$", "")
                .Replace("$", "")
                .Replace(" ", "")
                .Trim();

            var imagem = produto.ImagemPrincipal ?? produto.Imagem ?? "";
            var novaLinha = $"{gtin}|{nome}|{precoFormatado}|{imagem}";

            // Procura linha existente e atualiza, ou adiciona nova
            bool encontrado = false;
            for (int i = 0; i < linhas.Count; i++)
            {
                if (linhas[i].StartsWith($"{gtin}|"))
                {
                    linhas[i] = novaLinha;
                    encontrado = true;
                    break;
                }
            }

            if (!encontrado)
            {
                linhas.Add(novaLinha);
            }

            await File.WriteAllLinesAsync(_dataFilePath, linhas, Encoding.UTF8);

            _logger.LogInformation($"Produto atualizado no arquivo de dados: {produto.Nome}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao atualizar produto no arquivo de dados: {produto.Nome}");
            return false;
        }
    }

    /// <summary>
    /// Retorna o caminho do arquivo de dados
    /// </summary>
    public string GetDataFilePath()
    {
        return _dataFilePath;
    }

    /// <summary>
    /// Verifica se o arquivo existe
    /// </summary>
    public bool FileExists()
    {
        return File.Exists(_dataFilePath);
    }
}


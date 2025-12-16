using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;
using OlistGertecIntegration.Services;

namespace OlistGertecIntegration.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigController> _logger;
    private readonly string _envPath;
    private readonly OlistApiService _olistService;
    private readonly DatabaseService _databaseService;

    public ConfigController(
        IConfiguration configuration,
        ILogger<ConfigController> logger,
        OlistApiService olistService,
        DatabaseService databaseService)
    {
        _configuration = configuration;
        _logger = logger;
        _olistService = olistService;
        _databaseService = databaseService;
        
        // Usa arquivo .env na raiz do projeto
        _envPath = Path.Combine(AppContext.BaseDirectory, ".env");
        
        // Se não existir na raiz, tenta no diretório atual
        if (!System.IO.File.Exists(_envPath))
        {
            _envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        }
    }

    [HttpGet]
    public IActionResult GetConfig()
    {
        try
        {
            var config = new
            {
                olistApi = new
                {
                    baseUrl = _configuration["OlistApi:BaseUrl"],
                    token = MaskToken(_configuration["OlistApi:Token"]),
                    format = _configuration["OlistApi:Format"]
                },
                database = new
                {
                    host = _configuration["Database:Host"],
                    database = _configuration["Database:Database"],
                    tableName = _configuration["Database:TableName"]
                },
                priceMonitoring = new
                {
                    enabled = _configuration["PriceMonitoring:Enabled"],
                    checkIntervalMinutes = _configuration["PriceMonitoring:CheckIntervalMinutes"]
                }
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter configurações");
            return StatusCode(500, new { message = "Erro ao obter configurações", error = ex.Message });
        }
    }


    [HttpPut("olist/token")]
    public async Task<IActionResult> UpdateOlistToken([FromBody] UpdateTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { message = "Token é obrigatório" });
            }

            // Atualiza arquivo .env
            await UpdateEnvFileAsync("OLIST_API__TOKEN", request.Token);

            // Atualiza token no serviço em tempo de execução
            _olistService.UpdateToken(request.Token);

            _logger.LogInformation("Token da API Olist atualizado");

            return Ok(new
            {
                message = "Token atualizado com sucesso",
                token = MaskToken(request.Token)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar token");
            return StatusCode(500, new { message = "Erro ao atualizar token", error = ex.Message });
        }
    }

    [HttpPost("database/refresh")]
    public async Task<IActionResult> RefreshDatabase()
    {
        try
        {
            _logger.LogInformation("Iniciando atualização manual do banco de dados...");
            
            var produtos = await _olistService.GetAllProductsAsync(null);
            
            if (produtos == null || produtos.Count == 0)
            {
                return BadRequest(new { message = "Nenhum produto encontrado na API" });
            }

            var (inseridos, atualizados, erros) = await _databaseService.UpsertProductsAsync(produtos);
            
            return Ok(new 
            { 
                message = "Banco de dados atualizado com sucesso",
                inseridos = inseridos,
                atualizados = atualizados,
                erros = erros,
                total = produtos.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar banco de dados");
            return StatusCode(500, new { message = "Erro ao atualizar banco de dados", error = ex.Message });
        }
    }

    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        try
        {
            var stats = _olistService.GetCacheStats();
            return Ok(new
            {
                apiStatus = "online",
                totalProdutos = stats.TotalProdutos,
                produtosPorGtin = stats.ProdutosPorGtin,
                produtosPorCodigo = stats.ProdutosPorCodigo,
                ultimaAtualizacao = stats.UltimaAtualizacao,
                cachePreCarregado = stats.CachePreCarregado
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas");
            return StatusCode(500, new { message = "Erro ao obter estatísticas", error = ex.Message });
        }
    }

    private async Task UpdateEnvFileAsync(string key, string value)
    {
        try
        {
            var lines = new List<string>();
            
            // Se o arquivo existe, lê as linhas
            if (System.IO.File.Exists(_envPath))
            {
                lines = (await System.IO.File.ReadAllLinesAsync(_envPath)).ToList();
            }

            // Procura se a chave já existe
            bool keyFound = false;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                // Ignora comentários e linhas vazias
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                // Verifica se a linha contém a chave
                if (line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(key + " =", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $"{key}={value}";
                    keyFound = true;
                    break;
                }
            }

            // Se não encontrou, adiciona no final
            if (!keyFound)
            {
                lines.Add($"{key}={value}");
            }

            // Salva o arquivo
            await System.IO.File.WriteAllLinesAsync(_envPath, lines);

            // Atualiza variável de ambiente em tempo de execução
            Environment.SetEnvironmentVariable(key, value);

            _logger.LogInformation($"Configuração {key} atualizada no .env");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao atualizar arquivo .env: {key}");
            throw;
        }
    }

    private string MaskToken(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length < 8)
        {
            return "***";
        }
        return token.Substring(0, 4) + "..." + token.Substring(token.Length - 4);
    }
}

public class UpdateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}


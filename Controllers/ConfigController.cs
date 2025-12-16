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
    private readonly GertecProtocolService _gertecService;
    private readonly OlistApiService _olistService;

    public ConfigController(
        IConfiguration configuration,
        ILogger<ConfigController> logger,
        GertecProtocolService gertecService,
        OlistApiService olistService)
    {
        _configuration = configuration;
        _logger = logger;
        _gertecService = gertecService;
        _olistService = olistService;
        
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
                gertec = new
                {
                    ipAddress = _configuration["Gertec:IpAddress"],
                    port = _configuration["Gertec:Port"],
                    reconnectIntervalSeconds = _configuration["Gertec:ReconnectIntervalSeconds"],
                    responseTimeoutMilliseconds = _configuration["Gertec:ResponseTimeoutMilliseconds"],
                    connectionTimeoutMilliseconds = _configuration["Gertec:ConnectionTimeoutMilliseconds"],
                    isConnected = _gertecService.IsConnected
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

    [HttpPut("gertec/ip")]
    public async Task<IActionResult> UpdateGertecIp([FromBody] UpdateIpRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.IpAddress))
            {
                return BadRequest(new { message = "IP Address é obrigatório" });
            }

            // Valida formato de IP
            if (!System.Net.IPAddress.TryParse(request.IpAddress, out _))
            {
                return BadRequest(new { message = "Formato de IP inválido" });
            }

            // Atualiza arquivo .env
            // NOTA: Agora é o IP do SERVIDOR Gertec (onde o terminal está rodando)
            await UpdateEnvFileAsync("GERTEC__IP_ADDRESS", request.IpAddress);

            // Reconecta ao servidor Gertec com novo IP
            await _gertecService.DisconnectAsync();
            await Task.Delay(1000);
            var connected = await _gertecService.ConnectAsync();

            _logger.LogInformation($"IP do servidor Gertec atualizado para: {request.IpAddress}");

            return Ok(new
            {
                message = "IP do servidor Gertec atualizado com sucesso",
                ipAddress = request.IpAddress,
                connected = connected
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar IP do servidor");
            return StatusCode(500, new { message = "Erro ao atualizar IP", error = ex.Message });
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

    [HttpPost("gertec/reconnect")]
    public async Task<IActionResult> ReconnectGertec()
    {
        try
        {
            // Reconecta ao servidor Gertec
            var connected = await _gertecService.ReconnectAsync();

            if (connected)
            {
                return Ok(new { message = "Reconectado ao servidor Gertec com sucesso", connected = true });
            }
            return BadRequest(new { message = "Falha ao reconectar ao servidor Gertec", connected = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao reconectar ao servidor Gertec");
            return StatusCode(500, new { message = "Erro ao reconectar", error = ex.Message });
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

public class UpdateIpRequest
{
    public string IpAddress { get; set; } = string.Empty;
}

public class UpdateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}


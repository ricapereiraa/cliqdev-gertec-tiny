using Microsoft.AspNetCore.Mvc;
using OlistGertecIntegration.Models;
using OlistGertecIntegration.Services;

namespace OlistGertecIntegration.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IntegrationController : ControllerBase
{
    private readonly ILogger<IntegrationController> _logger;
    private readonly GertecProtocolService _gertecService;
    private readonly OlistApiService _olistService;

    public IntegrationController(
        ILogger<IntegrationController> logger,
        GertecProtocolService gertecService,
        OlistApiService olistService)
    {
        _logger = logger;
        _gertecService = gertecService;
        _olistService = olistService;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            gertecConnected = _gertecService.IsConnected,
            timestamp = DateTime.Now
        });
    }

    [HttpPost("gertec/connect")]
    public async Task<IActionResult> ConnectGertec()
    {
        var connected = await _gertecService.ConnectAsync();
        if (connected)
        {
            return Ok(new { message = "Conectado ao Gertec com sucesso" });
        }
        return BadRequest(new { message = "Falha ao conectar ao Gertec" });
    }

    [HttpPost("gertec/disconnect")]
    public async Task<IActionResult> DisconnectGertec()
    {
        await _gertecService.DisconnectAsync();
        return Ok(new { message = "Desconectado do Gertec" });
    }

    [HttpPost("gertec/message")]
    public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
    {
        if (string.IsNullOrEmpty(request.Linha1) || string.IsNullOrEmpty(request.Linha2))
        {
            return BadRequest(new { message = "Linha1 e Linha2 são obrigatórias" });
        }

        var sent = await _gertecService.SendMessageAsync(
            request.Linha1, 
            request.Linha2, 
            request.TempoSegundos);

        if (sent)
        {
            return Ok(new { message = "Mensagem enviada com sucesso" });
        }
        return BadRequest(new { message = "Falha ao enviar mensagem" });
    }

    [HttpGet("gertec/macaddress")]
    public async Task<IActionResult> GetMacAddress()
    {
        var macAddress = await _gertecService.GetMacAddressAsync();
        if (macAddress != null)
        {
            return Ok(new { macAddress });
        }
        return BadRequest(new { message = "Falha ao obter MAC Address" });
    }

    [HttpGet("product/{barcode}")]
    public async Task<IActionResult> GetProduct(string barcode)
    {
        var produto = await _olistService.GetProductByBarcodeAsync(barcode);
        if (produto != null)
        {
            return Ok(produto);
        }
        return NotFound(new { message = "Produto não encontrado" });
    }

    [HttpPost("product/{barcode}/send")]
    public async Task<IActionResult> SendProductToGertec(string barcode)
    {
        var produto = await _olistService.GetProductByBarcodeAsync(barcode);
        if (produto == null)
        {
            return NotFound(new { message = "Produto não encontrado" });
        }

        var nomeFormatado = produto.Nome.PadRight(80).Substring(0, Math.Min(80, produto.Nome.Length));
        var preco = !string.IsNullOrEmpty(produto.PrecoPromocional) 
            ? produto.PrecoPromocional 
            : produto.Preco;
        var precoFormatado = _olistService.FormatPrice(preco);

        var sent = await _gertecService.SendProductInfoAsync(nomeFormatado, precoFormatado);
        if (sent)
        {
            return Ok(new { message = "Produto enviado ao Gertec com sucesso" });
        }
        return BadRequest(new { message = "Falha ao enviar produto ao Gertec" });
    }

    [HttpPost("prices/sync")]
    public async Task<IActionResult> SyncPricesFromTinyErp()
    {
        try
        {
            _logger.LogInformation("Iniciando sincronização manual de preços do Tiny ERP...");

            if (!_gertecService.IsConnected)
            {
                _logger.LogWarning("Gertec não conectado. Tentando conectar...");
                var connected = await _gertecService.ConnectAsync();
                if (!connected)
                {
                    return BadRequest(new { 
                        message = "Não foi possível conectar ao Gertec. Verifique a conexão de rede.",
                        gertecConnected = false
                    });
                }
            }

            // Busca todos os produtos do Tiny ERP
            var produtos = await _olistService.GetAllProductsAsync(null);
            
            if (produtos == null || produtos.Count == 0)
            {
                return Ok(new { 
                    message = "Nenhum produto encontrado no Tiny ERP",
                    produtosSincronizados = 0
                });
            }

            int produtosEnviados = 0;
            int produtosComErro = 0;
            var erros = new List<string>();

            foreach (var produto in produtos)
            {
                // Usa código ou GTIN como chave
                var chaveProduto = !string.IsNullOrEmpty(produto.Codigo) 
                    ? produto.Codigo 
                    : produto.Gtin;
                
                if (string.IsNullOrEmpty(chaveProduto))
                    continue;

                try
                {
                    // Formata nome para 4 linhas x 20 colunas (80 bytes)
                    var nomeFormatado = FormatProductName(produto.Nome);
                    
                    // Usa preço promocional se disponível e maior que zero, senão usa preço normal
                    var preco = !string.IsNullOrEmpty(produto.PrecoPromocional) && 
                               decimal.TryParse(produto.PrecoPromocional, out var precoPromo) && 
                               precoPromo > 0
                        ? produto.PrecoPromocional 
                        : produto.Preco;
                    
                    var precoFormatado = _olistService.FormatPrice(preco);

                    // Envia para o Gertec
                    bool enviado = await _gertecService.SendProductInfoAsync(nomeFormatado, precoFormatado);
                    
                    if (enviado)
                    {
                        produtosEnviados++;
                    }
                    else
                    {
                        produtosComErro++;
                        erros.Add($"Falha ao enviar produto {produto.Nome} (Código: {chaveProduto})");
                    }
                }
                catch (Exception ex)
                {
                    produtosComErro++;
                    erros.Add($"Erro ao processar produto {produto.Nome} (Código: {chaveProduto}): {ex.Message}");
                    _logger.LogError(ex, $"Erro ao sincronizar produto {produto.Nome}");
                }
            }

            _logger.LogInformation($"Sincronização concluída: {produtosEnviados} produtos enviados, {produtosComErro} com erro");

            return Ok(new
            {
                message = "Sincronização concluída",
                totalProdutos = produtos.Count,
                produtosEnviados,
                produtosComErro,
                erros = erros.Take(10).ToList() // Limita a 10 erros na resposta
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na sincronização manual de preços");
            return StatusCode(500, new { 
                message = "Erro ao sincronizar preços",
                error = ex.Message
            });
        }
    }

    private string FormatProductName(string nome)
    {
        // Formata para 4 linhas x 20 colunas (80 bytes total)
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
}

public class MessageRequest
{
    public string Linha1 { get; set; } = string.Empty;
    public string Linha2 { get; set; } = string.Empty;
    public int TempoSegundos { get; set; } = 5;
}


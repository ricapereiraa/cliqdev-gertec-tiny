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
}

public class MessageRequest
{
    public string Linha1 { get; set; } = string.Empty;
    public string Linha2 { get; set; } = string.Empty;
    public int TempoSegundos { get; set; } = 5;
}


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
    private readonly GertecDataFileService _dataFileService;

    public IntegrationController(
        ILogger<IntegrationController> logger,
        GertecProtocolService gertecService,
        OlistApiService olistService,
        GertecDataFileService dataFileService)
    {
        _logger = logger;
        _gertecService = gertecService;
        _olistService = olistService;
        _dataFileService = dataFileService;
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
            _logger.LogInformation("Iniciando sincronização manual de produtos do Tiny ERP para arquivo...");
            Console.WriteLine("Iniciando sincronização manual de produtos do Tiny ERP para arquivo...");

            // Regenera arquivo completo com todos os produtos
            var sucesso = await _dataFileService.GenerateDataFileAsync();
            
            if (sucesso)
            {
                return Ok(new
                {
                    message = "Arquivo de dados Gertec atualizado com sucesso",
                    filePath = _dataFileService.GetDataFilePath(),
                    fileExists = _dataFileService.FileExists()
                });
            }
            
            return BadRequest(new { message = "Falha ao atualizar arquivo de dados Gertec" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na sincronização manual de produtos");
            return StatusCode(500, new { 
                message = "Erro ao sincronizar produtos",
                error = ex.Message
            });
        }
    }

    [HttpPost("gertec/datafile/generate")]
    public async Task<IActionResult> GenerateDataFile()
    {
        try
        {
            _logger.LogInformation("Gerando arquivo de dados Gertec manualmente...");
            var success = await _dataFileService.GenerateDataFileAsync();
            
            if (success)
            {
                return Ok(new
                {
                    message = "Arquivo de dados Gertec gerado com sucesso",
                    filePath = _dataFileService.GetDataFilePath(),
                    fileExists = _dataFileService.FileExists()
                });
            }
            
            return BadRequest(new { message = "Falha ao gerar arquivo de dados Gertec" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar arquivo de dados Gertec");
            return StatusCode(500, new { message = "Erro ao gerar arquivo de dados", error = ex.Message });
        }
    }

    [HttpGet("gertec/datafile/info")]
    public IActionResult GetDataFileInfo()
    {
        try
        {
            return Ok(new
            {
                filePath = _dataFileService.GetDataFilePath(),
                fileExists = _dataFileService.FileExists(),
                fileSize = _dataFileService.FileExists() 
                    ? new FileInfo(_dataFileService.GetDataFilePath()).Length 
                    : 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter informações do arquivo de dados");
            return StatusCode(500, new { message = "Erro ao obter informações", error = ex.Message });
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


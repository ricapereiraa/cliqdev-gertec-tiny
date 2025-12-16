using Microsoft.AspNetCore.Mvc;
using OlistGertecIntegration.Models;
using OlistGertecIntegration.Services;

namespace OlistGertecIntegration.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IntegrationController : ControllerBase
{
    private readonly ILogger<IntegrationController> _logger;
    private readonly OlistApiService _olistService;
    private readonly DatabaseService _databaseService;

    public IntegrationController(
        ILogger<IntegrationController> logger,
        OlistApiService olistService,
        DatabaseService databaseService)
    {
        _logger = logger;
        _olistService = olistService;
        _databaseService = databaseService;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            serviceRunning = true,
            timestamp = DateTime.Now
        });
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

    [HttpPost("prices/sync")]
    public async Task<IActionResult> SyncPricesFromTinyErp()
    {
        try
        {
            _logger.LogInformation("Iniciando sincronização manual de produtos do Tiny ERP para banco de dados...");
            Console.WriteLine("Iniciando sincronização manual de produtos do Tiny ERP para banco de dados...");

            var produtos = await _olistService.GetAllProductsAsync(null);
            
            if (produtos == null || produtos.Count == 0)
            {
                return BadRequest(new { message = "Nenhum produto encontrado na API" });
            }

            var (inseridos, atualizados, erros) = await _databaseService.UpsertProductsAsync(produtos);
            
            return Ok(new
            {
                message = "Produtos sincronizados com sucesso",
                inseridos = inseridos,
                atualizados = atualizados,
                erros = erros,
                total = produtos.Count
            });
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
}


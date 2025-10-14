using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Pos;

namespace MultiTenantEcommerce.Presentation.Controllers;

[ApiController]
[Route("api/pos")]
[Authorize]
public class PointOfSaleController : ControllerBase
{
    private readonly IPointOfSaleService _pointOfSaleService;

    public PointOfSaleController(IPointOfSaleService pointOfSaleService)
    {
        _pointOfSaleService = pointOfSaleService;
    }

    [HttpPost("sales")]
    [ProducesResponseType(typeof(PosSaleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSale([FromBody] PosSaleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var sale = await _pointOfSaleService.CreateOfflineSaleAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetReceipt), new { orderId = sale.OrderId }, sale);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("inventory/sync")]
    [ProducesResponseType(typeof(IEnumerable<InventorySyncResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SyncInventory([FromBody] IEnumerable<InventorySyncRequest> request, CancellationToken cancellationToken)
    {
        try
        {
            var results = await _pointOfSaleService.SyncInventoryAsync(request, cancellationToken);
            return Ok(results);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("sales/{orderId:guid}/receipt")]
    [ProducesResponseType(typeof(ReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReceipt(Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            var receipt = await _pointOfSaleService.GenerateReceiptAsync(orderId, cancellationToken);
            return Ok(receipt);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

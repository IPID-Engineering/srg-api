using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Warehouses;

namespace SRG.Api.Controllers;

[ApiController]
[Route("warehouses")]
[Authorize]
public class WarehouseController(IWarehouseService warehouseService) : ControllerBase
{
    [HttpGet("main")]
    [Authorize(Roles = "Logistician")]
    public async Task<ActionResult<WarehouseResponse>> GetMain(CancellationToken cancellationToken)
    {
        return Ok(await warehouseService.GetMainWarehouseAsync(cancellationToken));
    }

    [HttpGet("my")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<WarehouseResponse>> GetMySubWarehouse(CancellationToken cancellationToken)
    {
        try
        {
            var crewIdClaim = User.FindFirst("crewId")?.Value;
            if (!string.IsNullOrEmpty(crewIdClaim) && Guid.TryParse(crewIdClaim, out var crewId))
            {
                return Ok(await warehouseService.GetSubWarehouseAsync(crewId, cancellationToken));
            }
            return Ok(await warehouseService.GetForemanWarehouseAsync(User.GetUserId(), cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("sub/{ownerId:guid}")]
    [Authorize(Roles = "Logistician")]
    public async Task<ActionResult<WarehouseResponse>> GetSubWarehouse(Guid ownerId, CancellationToken cancellationToken)
    {
        return Ok(await warehouseService.GetSubWarehouseAsync(ownerId, cancellationToken));
    }

    [HttpGet("sub")]
    [Authorize(Roles = "Logistician")]
    public async Task<ActionResult<List<WarehouseResponse>>> GetAllSubWarehouses(CancellationToken cancellationToken)
    {
        return Ok(await warehouseService.GetAllSubWarehousesAsync(cancellationToken));
    }

    [HttpGet("{id:guid}/stock")]
    [Authorize(Roles = "Logistician,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<List<StockResponse>>> GetStock(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await warehouseService.GetStockAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("{id:guid}/movements")]
    [Authorize(Roles = "Logistician")]
    public async Task<ActionResult<List<StockMovementResponse>>> GetMovements(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await warehouseService.GetMovementsAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}

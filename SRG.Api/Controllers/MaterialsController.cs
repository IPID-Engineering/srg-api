using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Application.Warehouses;

namespace SRG.Api.Controllers;

[ApiController]
[Route("materials")]
[Authorize]
public class MaterialsController(IMaterialService materialService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "PM,SPM,Logistician,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<List<MaterialResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await materialService.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Logistician")]
    public async Task<ActionResult<MaterialResponse>> Create(
        CreateMaterialRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await materialService.CreateMaterialAsync(request, cancellationToken));
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Logistician")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await materialService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("{id:guid}/check-availability")]
    [Authorize(Roles = "PM,SPM,Logistician,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<MaterialAvailabilityResponse>> CheckAvailability(
        Guid id,
        CheckMaterialAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await materialService.CheckAvailabilityAsync(id, request, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}

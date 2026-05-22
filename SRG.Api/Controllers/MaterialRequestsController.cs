using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.MaterialRequests;

namespace SRG.Api.Controllers;

[ApiController]
[Route("material-requests")]
public class MaterialRequestsController(IMaterialRequestService materialRequestService) : ControllerBase
{
    [HttpPost("work-order/{workOrderId}")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<MaterialRequestResponse>> CreateRequest(
        Guid workOrderId,
        CreateMaterialRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Quantity <= 0)
            {
                return BadRequest(new { message = "Ilość musi być większa od zera." });
            }
            
            var userId = User.GetUserId();
            var result = await materialRequestService.CreateRequestAsync(workOrderId, userId, request, cancellationToken);
            return Created($"/material-requests/{result.Id}", result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się utworzyć wniosku o materiał." });
        }
    }

    [HttpGet("work-order/{workOrderId}")]
    [Authorize(Roles = "PM,SPM,Admin,Foreman,SubcontractorForeman,Logistician")]
    public async Task<ActionResult<List<MaterialRequestResponse>>> GetByWorkOrder(
        Guid workOrderId,
        CancellationToken cancellationToken)
    {
        try
        {
            var requests = await materialRequestService.GetByWorkOrderAsync(workOrderId, cancellationToken);
            return Ok(requests);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się pobrać wniosków o materiały." });
        }
    }

    [HttpGet("pending")]
    [Authorize(Roles = "PM,SPM,Admin,Logistician")]
    public async Task<ActionResult<List<MaterialRequestResponse>>> GetPendingRequests(
        CancellationToken cancellationToken)
    {
        try
        {
            var requests = await materialRequestService.GetPendingRequestsAsync(cancellationToken);
            return Ok(requests);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się pobrać wniosków oczekujących." });
        }
    }

    [HttpPost("{requestId}/process")]
    [Authorize(Roles = "PM,SPM,Admin,Logistician")]
    public async Task<ActionResult<MaterialRequestResponse>> ProcessRequest(
        Guid requestId,
        ProcessMaterialRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await materialRequestService.ProcessRequestAsync(requestId, userId, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się przetworzyć wniosku." });
        }
    }

    [HttpDelete("{requestId}")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<IActionResult> DeleteRequest(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            await materialRequestService.DeleteRequestAsync(requestId, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się usunąć wniosku." });
        }
    }
}

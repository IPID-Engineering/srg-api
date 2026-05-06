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
        var userId = User.GetUserId();
        var result = await materialRequestService.CreateRequestAsync(workOrderId, userId, request, cancellationToken);
        return Created($"/material-requests/{result.Id}", result);
    }

    [HttpGet("work-order/{workOrderId}")]
    [Authorize(Roles = "PM,SPM,Admin,Foreman,SubcontractorForeman,Logistician")]
    public async Task<ActionResult<List<MaterialRequestResponse>>> GetByWorkOrder(
        Guid workOrderId,
        CancellationToken cancellationToken)
    {
        var requests = await materialRequestService.GetByWorkOrderAsync(workOrderId, cancellationToken);
        return Ok(requests);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "PM,SPM,Admin,Logistician")]
    public async Task<ActionResult<List<MaterialRequestResponse>>> GetPendingRequests(
        CancellationToken cancellationToken)
    {
        var requests = await materialRequestService.GetPendingRequestsAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpPost("{requestId}/process")]
    [Authorize(Roles = "PM,SPM,Admin,Logistician")]
    public async Task<ActionResult<MaterialRequestResponse>> ProcessRequest(
        Guid requestId,
        ProcessMaterialRequestRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await materialRequestService.ProcessRequestAsync(requestId, userId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{requestId}")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<IActionResult> DeleteRequest(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        await materialRequestService.DeleteRequestAsync(requestId, cancellationToken);
        return NoContent();
    }
}

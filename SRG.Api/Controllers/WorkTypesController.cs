using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Application.WorkOrders;

namespace SRG.Api.Controllers;

[ApiController]
[Route("work-types")]
[Authorize]
public class WorkTypesController(IWorkTypeService workTypeService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,PM,SPM,Foreman,SubcontractorForeman,Subcontractor")]
    public async Task<ActionResult<List<WorkTypeResponse>>> Get(
        [FromQuery] bool activeOnly,
        CancellationToken cancellationToken)
    {
        return Ok(await workTypeService.GetWorkTypesAsync(activeOnly, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,PM,SPM")]
    public async Task<ActionResult<WorkTypeResponse>> Create(WorkTypeRequest request, CancellationToken cancellationToken)
    {
        return await WriteAction(async () => Created(string.Empty, await workTypeService.CreateWorkTypeAsync(request, cancellationToken)));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,PM,SPM")]
    public async Task<ActionResult<WorkTypeResponse>> Update(Guid id, WorkTypeRequest request, CancellationToken cancellationToken)
    {
        return await WriteAction(async () => Ok(await workTypeService.UpdateWorkTypeAsync(id, request, cancellationToken)));
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin,PM,SPM")]
    public async Task<ActionResult<WorkTypeResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        return await WriteAction(async () => Ok(await workTypeService.DeactivateWorkTypeAsync(id, cancellationToken)));
    }

    private async Task<ActionResult<WorkTypeResponse>> WriteAction(Func<Task<ActionResult<WorkTypeResponse>>> action)
    {
        try
        {
            return await action();
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
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.WorkOrders;

namespace SRG.Api.Controllers;

[ApiController]
[Route("work-orders")]
[Authorize]
public class WorkOrdersController(IWorkOrderService workOrderService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "PM,SPM,Foreman,SubcontractorForeman,Subcontractor,Logistician")]
    public async Task<ActionResult<List<WorkOrderResponse>>> Get(CancellationToken cancellationToken)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        return Ok(await workOrderService.GetWorkOrdersAsync(User.GetUserId(), role, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "PM,SPM,Foreman,SubcontractorForeman,Subcontractor,Logistician")]
    public async Task<ActionResult<WorkOrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await workOrderService.GetWorkOrderAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "PM,SPM")]
    public async Task<ActionResult<WorkOrderResponse>> Create(CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        return await WriteAction(() => workOrderService.CreateWorkOrderAsync(request, User.GetUserId(), cancellationToken), created: true);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "PM,SPM")]
    public async Task<ActionResult<WorkOrderResponse>> Update(Guid id, UpdateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        return await WriteAction(() => workOrderService.UpdateWorkOrderAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/ordered-works")]
    [Authorize(Roles = "PM,SPM")]
    public async Task<ActionResult<WorkOrderResponse>> AddOrderedWork(
        Guid id,
        AddOrderedWorkRequest request,
        CancellationToken cancellationToken)
    {
        return await WriteAction(() => workOrderService.AddOrderedWorkAsync(id, request, User.GetUserId(), cancellationToken));
    }

    [HttpPost("{id:guid}/ordered-materials")]
    [Authorize(Roles = "PM,SPM,Logistician")]
    public async Task<ActionResult<WorkOrderResponse>> AddOrderedMaterial(
        Guid id,
        AddOrderedMaterialRequest request,
        CancellationToken cancellationToken)
    {
        return await WriteAction(() => workOrderService.AddOrderedMaterialAsync(id, request, User.GetUserId(), cancellationToken));
    }

    [HttpDelete("{id:guid}/ordered-works/{orderedWorkId:guid}")]
    [Authorize(Roles = "PM,SPM")]
    public async Task<ActionResult> RemoveOrderedWork(Guid id, Guid orderedWorkId, CancellationToken cancellationToken)
    {
        try
        {
            await workOrderService.RemoveOrderedWorkAsync(id, orderedWorkId, User.GetUserId(), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpDelete("{id:guid}/ordered-materials/{orderedMaterialId:guid}")]
    [Authorize(Roles = "PM,SPM,Logistician")]
    public async Task<ActionResult> RemoveOrderedMaterial(Guid id, Guid orderedMaterialId, CancellationToken cancellationToken)
    {
        try
        {
            await workOrderService.RemoveOrderedMaterialAsync(id, orderedMaterialId, User.GetUserId(), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{id:guid}/progress")]
    [Authorize(Roles = "PM,SPM,Foreman,SubcontractorForeman,Subcontractor,Logistician")]
    public async Task<ActionResult<WorkOrderProgressResponse>> GetProgress(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await workOrderService.GetProgressAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<WorkOrderResponse>> AcceptWorkOrder(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await workOrderService.AcceptWorkOrderAsync(id, User.GetUserId(), cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private async Task<ActionResult<WorkOrderResponse>> WriteAction(
        Func<Task<WorkOrderResponse>> action,
        bool created = false)
    {
        try
        {
            var response = await action();
            return created ? Created(string.Empty, response) : Ok(response);
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

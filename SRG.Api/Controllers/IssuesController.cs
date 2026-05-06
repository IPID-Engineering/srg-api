using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Warehouses;

namespace SRG.Api.Controllers;

[ApiController]
[Route("issues")]
[Authorize]
public class IssuesController(IIssueService issueService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Logistician")]
    public async Task<ActionResult<List<IssueResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await issueService.GetAllAsync(cancellationToken));
    }

    [HttpGet("by-work-order/{workOrderId:guid}")]
    [Authorize(Roles = "Logistician,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<List<IssueResponse>>> GetByWorkOrder(Guid workOrderId, CancellationToken cancellationToken)
    {
        return Ok(await issueService.GetByWorkOrderAsync(workOrderId, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Logistician")]
    public async Task<ActionResult<IssueResponse>> Create(
        CreateIssueRequest request,
        CancellationToken cancellationToken)
    {
        return await Handle(() => issueService.CreateIssueAsync(request, User.GetUserId(), cancellationToken));
    }

    [HttpPost("{id:guid}/item")]
    [Authorize(Roles = "Logistician")]
    public async Task<ActionResult<IssueResponse>> AddItem(
        Guid id,
        AddIssueItemRequest request,
        CancellationToken cancellationToken)
    {
        return await Handle(() => issueService.AddItemAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "Logistician")]
    public async Task<ActionResult<IssueResponse>> Confirm(Guid id, CancellationToken cancellationToken)
    {
        return await Handle(() => issueService.ConfirmIssueAsync(id, cancellationToken));
    }

    private static async Task<ActionResult<IssueResponse>> Handle(Func<Task<IssueResponse>> action)
    {
        try
        {
            return await action();
        }
        catch (ValidationException exception)
        {
            return new BadRequestObjectResult(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return new NotFoundObjectResult(new { message = exception.Message });
        }
    }
}

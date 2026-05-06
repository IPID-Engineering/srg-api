using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Warehouses;

namespace SRG.Api.Controllers;

[ApiController]
[Route("returns")]
[Authorize]
public class ReturnsController(IReturnService returnService) : ControllerBase
{
    [HttpGet("submitted")]
    [Authorize(Roles = "Logistician")]
    public async Task<ActionResult<List<ReturnResponse>>> GetSubmitted(CancellationToken cancellationToken)
    {
        return Ok(await returnService.GetSubmittedAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<ReturnResponse>> Create(CancellationToken cancellationToken)
    {
        return Ok(await returnService.CreateReturnAsync(User.GetUserId(), cancellationToken));
    }

    [HttpPost("{id:guid}/item")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<ReturnResponse>> AddItem(
        Guid id,
        AddReturnItemRequest request,
        CancellationToken cancellationToken)
    {
        return await Handle(() => returnService.AddItemAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<ReturnResponse>> Submit(Guid id, CancellationToken cancellationToken)
    {
        return await Handle(() => returnService.SubmitAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Logistician")]
    public async Task<ActionResult<ReturnResponse>> Approve(Guid id, CancellationToken cancellationToken)
    {
        return await Handle(() => returnService.ApproveAsync(id, cancellationToken));
    }

    private static async Task<ActionResult<ReturnResponse>> Handle(Func<Task<ReturnResponse>> action)
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

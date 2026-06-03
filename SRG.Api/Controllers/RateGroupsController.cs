using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.RateGroups;

namespace SRG.Api.Controllers;

[ApiController]
[Route("rate-groups")]
[Authorize(Roles = "Subcontractor")]
public class RateGroupsController(IRateGroupService rateGroupService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RateGroupResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await rateGroupService.GetAllAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RateGroupResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await rateGroupService.GetByIdAsync(id, User.GetUserId(), cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<RateGroupResponse>> Create(
        CreateRateGroupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await rateGroupService.CreateAsync(User.GetUserId(), request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RateGroupResponse>> Update(
        Guid id,
        UpdateRateGroupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await rateGroupService.UpdateAsync(id, User.GetUserId(), request, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await rateGroupService.DeleteAsync(id, User.GetUserId(), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPost("assign-worker")]
    public async Task<ActionResult> AssignWorker(
        [FromBody] AssignWorkerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await rateGroupService.AssignWorkerAsync(request.WorkerId, request.RateGroupId, User.GetUserId(), cancellationToken);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }
}

public record AssignWorkerRequest(Guid WorkerId, Guid? RateGroupId);

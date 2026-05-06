using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Construction;

namespace SRG.Api.Controllers;

[ApiController]
[Route("subcontractor/workers")]
[Authorize(Roles = "Subcontractor")]
public class SubcontractorWorkersController(ISubcontractorWorkerService subcontractorWorkerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SubcontractorWorkerResponse>>> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await subcontractorWorkerService.GetMineAsync(User.GetUserId(), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<SubcontractorWorkerResponse>> Create(
        CreateSubcontractorWorkerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await subcontractorWorkerService.CreateAsync(request, User.GetUserId(), cancellationToken));
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubcontractorWorkerResponse>> Update(
        Guid id,
        UpdateSubcontractorWorkerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await subcontractorWorkerService.UpdateAsync(id, request, User.GetUserId(), cancellationToken));
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await subcontractorWorkerService.RemoveAsync(id, User.GetUserId(), cancellationToken);
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
}

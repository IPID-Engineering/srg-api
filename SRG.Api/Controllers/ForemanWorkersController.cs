using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Construction;

namespace SRG.Api.Controllers;

[ApiController]
[Route("foreman/workers")]
[Authorize(Roles = "SubcontractorForeman")]
public class ForemanWorkersController(IForemanWorkerService foremanWorkerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ForemanWorkerResponse>>> GetMyWorkers(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await foremanWorkerService.GetMyWorkersAsync(User.GetUserId(), cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ForemanWorkerResponse>> AddWorker(
        AddForemanWorkerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await foremanWorkerService.AddWorkerAsync(request, User.GetUserId(), cancellationToken));
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
    public async Task<IActionResult> RemoveWorker(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await foremanWorkerService.RemoveWorkerAsync(id, User.GetUserId(), cancellationToken);
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

    [HttpGet("{id:guid}/stats")]
    public async Task<ActionResult<ForemanWorkerStatsResponse>> GetWorkerStats(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await foremanWorkerService.GetWorkerStatsAsync(id, User.GetUserId(), cancellationToken));
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

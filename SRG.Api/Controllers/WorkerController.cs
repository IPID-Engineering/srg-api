using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Construction;

namespace SRG.Api.Controllers;

[ApiController]
[Route("workers")]
[Authorize(Roles = "Foreman,SubcontractorForeman")]
public class WorkerController(IWorkerService workerService) : ControllerBase
{
    [HttpGet("crew/{crewId:guid}")]
    public async Task<ActionResult<List<WorkerResponse>>> GetByCrew(
        Guid crewId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await workerService.GetByCrewAsync(crewId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<WorkerResponse>> AddPerson(
        AddPersonRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await workerService.AddPersonAsync(request, User.GetUserId(), cancellationToken));
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
    public async Task<IActionResult> RemovePerson(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await workerService.RemovePersonAsync(id, User.GetUserId(), cancellationToken);
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

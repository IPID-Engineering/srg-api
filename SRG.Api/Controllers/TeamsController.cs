using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Application.Construction;

namespace SRG.Api.Controllers;

[ApiController]
[Route("teams")]
[Authorize(Roles = "Foreman,SubcontractorForeman")]
public class TeamsController(ITeamService teamService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TeamResponse>>> GetByCrew(
        [FromQuery] Guid crewId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await teamService.GetByCrewAsync(crewId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TeamResponse>> Create(
        CreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await teamService.CreateTeamAsync(request, cancellationToken));
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

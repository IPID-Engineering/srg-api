using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Construction;

namespace SRG.Api.Controllers;

[ApiController]
[Route("crew-access")]
[Authorize(Roles = "Admin,SPM")]
public class CrewAccessController(ICrewAccessService crewAccessService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CrewWithAccessResponse>>> GetAllCrewsWithAccess(CancellationToken cancellationToken)
    {
        return Ok(await crewAccessService.GetAllCrewsWithAccessAsync(cancellationToken));
    }

    [HttpGet("{crewId:guid}")]
    public async Task<ActionResult<CrewWithAccessResponse>> GetCrewWithAccess(Guid crewId, CancellationToken cancellationToken)
    {
        return Ok(await crewAccessService.GetCrewWithAccessAsync(crewId, cancellationToken));
    }

    [HttpPost("{crewId:guid}/users")]
    public async Task<ActionResult<CrewAccessResponse>> AssignAccess(
        Guid crewId,
        AssignCrewAccessRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId();
        return Ok(await crewAccessService.AssignAccessAsync(crewId, request.UserId, adminId, cancellationToken));
    }

    [HttpDelete("{crewId:guid}/users/{userId:guid}")]
    public async Task<IActionResult> RemoveAccess(Guid crewId, Guid userId, CancellationToken cancellationToken)
    {
        await crewAccessService.RemoveAccessAsync(crewId, userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{crewId:guid}/users/bulk")]
    public async Task<IActionResult> BulkAssignAccess(
        Guid crewId,
        BulkAssignCrewAccessRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId();
        await crewAccessService.BulkAssignAccessAsync(crewId, request.UserIds, adminId, cancellationToken);
        return NoContent();
    }
}

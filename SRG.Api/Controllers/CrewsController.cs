using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Construction;

namespace SRG.Api.Controllers;

[ApiController]
[Route("crews")]
[Authorize]
public class CrewsController(ICrewService crewService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "PM,SPM,Foreman,SubcontractorForeman,Subcontractor")]
    public async Task<ActionResult<List<CrewResponse>>> GetCrews(
        [FromQuery] Guid? projectId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (projectId.HasValue)
            {
                return Ok(await crewService.GetByProjectAsync(projectId.Value, cancellationToken));
            }

            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var userId = User.GetUserId();

            // PM widzi brygady, które stworzył LUB do których ma przypisany dostęp
            if (role == "PM")
            {
                var createdCrews = await crewService.GetByCreatorAsync(userId, cancellationToken);
                var accessCrews = await crewService.GetByUserAccessAsync(userId, cancellationToken);
                
                var allCrews = createdCrews
                    .Concat(accessCrews)
                    .DistinctBy(c => c.Id)
                    .OrderBy(c => c.Name)
                    .ToList();
                
                return Ok(allCrews);
            }

            // Subcontractor widzi tylko brygady, do których ma przypisany dostęp przez admina
            if (role == "Subcontractor")
            {
                return Ok(await crewService.GetByUserAccessAsync(userId, cancellationToken));
            }

            // SPM i Foreman widzą wszystkie brygady
            return Ok(await crewService.GetAllAsync(cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "PM")]
    public async Task<ActionResult<CrewResponse>> Create(
        CreateCrewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await crewService.CreateCrewAsync(request, User.GetUserId(), cancellationToken));
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

    [HttpPatch("{id:guid}/assign")]
    [Authorize(Roles = "PM")]
    public async Task<ActionResult<CrewResponse>> AssignToProject(
        Guid id,
        AssignCrewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await crewService.AssignToProjectAsync(id, request, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}

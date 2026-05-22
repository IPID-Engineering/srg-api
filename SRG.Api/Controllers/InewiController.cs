using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Inewi;
using SRG.Application.Persistence;

namespace SRG.Api.Controllers;

[ApiController]
[Route("inewi")]
[Authorize(Roles = "Subcontractor,SPM,PM,Admin")]
public class InewiController(
    IInewiService inewiService, 
    IConstructionRepository constructionRepository) : ControllerBase
{
    [HttpGet("crew/{subcontractorCrewId:guid}")]
    public async Task<ActionResult<List<InewiRecordResponse>>> GetBySubcontractorCrew(
        Guid subcontractorCrewId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await CanAccessCrewAsync(subcontractorCrewId, cancellationToken))
            {
                return Forbid();
            }
            return Ok(await inewiService.GetBySubcontractorCrewAsync(subcontractorCrewId, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się pobrać rekordów INEWI." });
        }
    }

    [HttpGet("crew/{subcontractorCrewId:guid}/range")]
    public async Task<ActionResult<List<InewiRecordResponse>>> GetByDateRange(
        Guid subcontractorCrewId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await CanAccessCrewAsync(subcontractorCrewId, cancellationToken))
            {
                return Forbid();
            }
            
            if (from > to)
            {
                return BadRequest(new { message = "Data początkowa nie może być późniejsza niż data końcowa." });
            }
            
            return Ok(await inewiService.GetByDateRangeAsync(subcontractorCrewId, from, to, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się pobrać rekordów INEWI." });
        }
    }

    [HttpPost("crew/{subcontractorCrewId:guid}/import")]
    public async Task<ActionResult<ImportInewiResult>> Import(
        Guid subcontractorCrewId,
        [FromBody] ImportInewiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await CanAccessCrewAsync(subcontractorCrewId, cancellationToken))
            {
                return Forbid();
            }
            
            if (request.Records == null || request.Records.Count == 0)
            {
                return BadRequest(new { message = "Brak rekordów do importu." });
            }
            
            if (request.Records.Count > 10000)
            {
                return BadRequest(new { message = "Maksymalnie 10000 rekordów w jednym imporcie." });
            }
            
            var userId = User.GetUserId();
            var result = await inewiService.ImportAsync(subcontractorCrewId, userId, request.Records, request.SourceFileName, cancellationToken);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się zaimportować rekordów INEWI." });
        }
    }
    
    private async Task<bool> CanAccessCrewAsync(Guid crewId, CancellationToken cancellationToken)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var userId = User.GetUserId();
        
        // Admin i SPM mają dostęp do wszystkich brygad
        if (role == "Admin" || role == "SPM")
        {
            return true;
        }
        
        var crew = await constructionRepository.GetSubcontractorCrewByIdAsync(crewId, cancellationToken);
        if (crew == null)
        {
            return false;
        }
        
        // Subcontractor może tylko do swoich brygad
        if (role == "Subcontractor")
        {
            return crew.SubcontractorId == userId;
        }
        
        // PM może tylko do brygad, do których ma dostęp
        if (role == "PM")
        {
            var hasAccess = await constructionRepository.GetSubcontractorCrewPmAccessAsync(crewId, userId, cancellationToken);
            return hasAccess != null;
        }
        
        return false;
    }
}

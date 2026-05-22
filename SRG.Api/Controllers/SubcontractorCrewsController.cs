using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Construction;
using SRG.Application.DailyReports;

namespace SRG.Api.Controllers;

[ApiController]
[Route("subcontractor/crews")]
[Authorize]
public class SubcontractorCrewsController(ISubcontractorCrewService crewService, IDailyReportService dailyReportService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Subcontractor")]
    public async Task<ActionResult<List<SubcontractorCrewResponse>>> GetMyCrews(CancellationToken cancellationToken)
    {
        return Ok(await crewService.GetMyCrewsAsync(User.GetUserId(), cancellationToken));
    }

    /// <summary>
    /// Get crews that PM has access to (granted by Subcontractor).
    /// Accessible by PM/SPM role only.
    /// </summary>
    [HttpGet("for-pm")]
    [Authorize(Roles = "PM,SPM")]
    public async Task<ActionResult<List<SubcontractorCrewResponse>>> GetCrewsForPm(CancellationToken cancellationToken)
    {
        return Ok(await crewService.GetCrewsForPmAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<ActionResult<SubcontractorCrewDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await crewService.GetByIdAsync(id, User.GetUserId(), cancellationToken));
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

    [HttpPost]
    [Authorize(Roles = "Subcontractor")]
    public async Task<ActionResult<SubcontractorCrewResponse>> Create(
        CreateSubcontractorCrewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await crewService.CreateAsync(request, User.GetUserId(), cancellationToken));
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<ActionResult<SubcontractorCrewResponse>> Update(
        Guid id,
        UpdateSubcontractorCrewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await crewService.UpdateAsync(id, request, User.GetUserId(), cancellationToken));
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

    [HttpPost("{id:guid}/foreman")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<IActionResult> SetForeman(Guid id, SetForemanRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await crewService.SetForemanAsync(id, request, User.GetUserId(), cancellationToken);
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

    [HttpPost("{id:guid}/workers/{workerId:guid}")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<IActionResult> AssignWorker(Guid id, Guid workerId, CancellationToken cancellationToken)
    {
        try
        {
            await crewService.AssignWorkerToCrewAsync(id, workerId, User.GetUserId(), cancellationToken);
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

    [HttpDelete("{id:guid}/workers/{workerId:guid}")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<IActionResult> RemoveWorker(Guid id, Guid workerId, CancellationToken cancellationToken)
    {
        try
        {
            await crewService.RemoveWorkerFromCrewAsync(id, workerId, User.GetUserId(), cancellationToken);
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

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await crewService.RemoveAsync(id, User.GetUserId(), cancellationToken);
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

    // PM Access Management

    [HttpGet("{id:guid}/pm-access")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<ActionResult<SubcontractorCrewWithPmAccessResponse>> GetCrewWithPmAccess(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await crewService.GetCrewWithPmAccessAsync(id, User.GetUserId(), cancellationToken));
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

    [HttpPost("{id:guid}/pm-access")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<ActionResult<SubcontractorCrewPmAccessResponse>> GrantPmAccess(
        Guid id,
        GrantPmAccessRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await crewService.GrantPmAccessAsync(id, request.PmUserId, User.GetUserId(), cancellationToken));
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

    [HttpDelete("{id:guid}/pm-access/{pmUserId:guid}")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<IActionResult> RevokePmAccess(Guid id, Guid pmUserId, CancellationToken cancellationToken)
    {
        try
        {
            await crewService.RevokePmAccessAsync(id, pmUserId, User.GetUserId(), cancellationToken);
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

    [HttpGet("{id:guid}/calendar")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<ActionResult<List<DailyReportCalendarResponse>>> GetCrewCalendar(
        Guid id,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        try
        {
            await crewService.GetByIdAsync(id, User.GetUserId(), cancellationToken);
            return Ok(await dailyReportService.GetCalendarAsync(id, year, month, cancellationToken));
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

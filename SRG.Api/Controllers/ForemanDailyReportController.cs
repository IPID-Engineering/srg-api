using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.DailyReports;

namespace SRG.Api.Controllers;

[ApiController]
[Route("foreman/daily-reports")]
[Authorize(Roles = "SubcontractorForeman")]
public class ForemanDailyReportController(IForemanDailyReportService foremanDailyReportService) : ControllerBase
{
    /// <summary>
    /// Pobiera lub tworzy DKP dla danego dnia. Automatycznie tworzy kartę jeśli nie istnieje.
    /// </summary>
    [HttpGet("date/{date}")]
    public async Task<ActionResult<ForemanDkpResponse>> GetOrCreateForDate(DateOnly date, CancellationToken cancellationToken)
    {
        try
        {
            var crewId = User.GetCrewIdRequired();
            var userId = User.GetUserId();
            return Ok(await foremanDailyReportService.GetOrCreateForDateAsync(date, crewId, userId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    /// <summary>
    /// Pobiera kalendarz DKP z automatycznym tworzeniem kart na 7 dni wstecz i 7 dni do przodu.
    /// </summary>
    [HttpGet("calendar")]
    public async Task<ActionResult<List<ForemanDkpCalendarItem>>> GetCalendar(CancellationToken cancellationToken)
    {
        try
        {
            var crewId = User.GetCrewIdRequired();
            var userId = User.GetUserId();
            return Ok(await foremanDailyReportService.GetCalendarWithAutoCreateAsync(crewId, userId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("{id:guid}/hours")]
    public async Task<ActionResult<ForemanDkpResponse>> AddHours(
        Guid id,
        AddForemanHoursRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var crewId = User.GetCrewIdRequired();
            return Ok(await foremanDailyReportService.AddHoursAsync(id, request, crewId, cancellationToken));
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

    [HttpPost("{id:guid}/work")]
    public async Task<ActionResult<ForemanDkpResponse>> AddWork(
        Guid id,
        AddForemanWorkRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var crewId = User.GetCrewIdRequired();
            return Ok(await foremanDailyReportService.AddWorkAsync(id, request, crewId, cancellationToken));
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

    [HttpPost("{id:guid}/materials")]
    public async Task<ActionResult<ForemanDkpResponse>> AddMaterial(
        Guid id,
        AddForemanMaterialRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var crewId = User.GetCrewIdRequired();
            return Ok(await foremanDailyReportService.AddMaterialAsync(id, request, crewId, cancellationToken));
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

    [HttpPut("{id:guid}/notes")]
    public async Task<ActionResult<ForemanDkpResponse>> UpdateNotes(
        Guid id,
        [FromBody] UpdateNotesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var crewId = User.GetCrewIdRequired();
            return Ok(await foremanDailyReportService.UpdateNotesAsync(id, request.Notes, crewId, cancellationToken));
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

    [HttpPut("{id:guid}/work-order")]
    public async Task<ActionResult<ForemanDkpResponse>> UpdateWorkOrder(
        Guid id,
        [FromBody] UpdateDkpWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var crewId = User.GetCrewIdRequired();
            return Ok(await foremanDailyReportService.UpdateWorkOrderAsync(id, request.WorkOrderId, crewId, cancellationToken));
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

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ForemanDkpResponse>> Submit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var crewId = User.GetCrewIdRequired();
            return Ok(await foremanDailyReportService.SubmitAsync(id, crewId, cancellationToken));
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

public record UpdateNotesRequest(string? Notes);
public record UpdateDkpWorkOrderRequest(Guid? WorkOrderId);

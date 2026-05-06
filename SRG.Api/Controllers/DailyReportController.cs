using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.DailyReports;

namespace SRG.Api.Controllers;

[ApiController]
[Route("daily-reports")]
[Authorize]
public class DailyReportController(IDailyReportService dailyReportService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<DailyReportResponse>> Create(
        CreateDailyReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await dailyReportService.CreateDailyReportAsync(request, User.GetUserId(), cancellationToken));
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

    [HttpGet("by-crew")]
    [Authorize(Roles = "Foreman,SubcontractorForeman,PM,SPM")]
    public async Task<ActionResult<List<DailyReportResponse>>> GetByCrew(
        [FromQuery] Guid crewId,
        CancellationToken cancellationToken)
    {
        return Ok(await dailyReportService.GetByCrewAsync(crewId, cancellationToken));
    }

    [HttpGet("by-work-order/{workOrderId:guid}")]
    [Authorize(Roles = "PM,SPM,Logistician,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<List<DailyReportResponse>>> GetByWorkOrder(
        Guid workOrderId,
        CancellationToken cancellationToken)
    {
        return Ok(await dailyReportService.GetByWorkOrderAsync(workOrderId, cancellationToken));
    }

    [HttpGet("submitted")]
    [Authorize(Roles = "PM,SPM")]
    public async Task<ActionResult<List<DailyReportResponse>>> GetSubmitted(CancellationToken cancellationToken)
    {
        return Ok(await dailyReportService.GetSubmittedAsync(cancellationToken));
    }

    [HttpGet("pm-review")]
    [Authorize(Roles = "PM")]
    public async Task<ActionResult<List<DailyReportResponse>>> GetForPmReview(CancellationToken cancellationToken)
    {
        return Ok(await dailyReportService.GetForPmReviewAsync(cancellationToken));
    }

    [HttpGet("spm-review")]
    [Authorize(Roles = "SPM")]
    public async Task<ActionResult<List<DailyReportResponse>>> GetForSpmReview(CancellationToken cancellationToken)
    {
        return Ok(await dailyReportService.GetForSpmReviewAsync(cancellationToken));
    }

    [HttpGet("subcontractor-review")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<ActionResult<List<DailyReportResponse>>> GetForSubcontractorReview(CancellationToken cancellationToken)
    {
        return Ok(await dailyReportService.GetForSubcontractorReviewAsync(cancellationToken));
    }

    [HttpGet("calendar")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<List<DailyReportCalendarResponse>>> GetCalendar(
        [FromQuery] Guid crewId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        return Ok(await dailyReportService.GetCalendarAsync(crewId, year, month, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Foreman,SubcontractorForeman,PM,SPM")]
    public async Task<ActionResult<DailyReportResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await dailyReportService.GetByIdAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPut("{id:guid}/notes")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<DailyReportResponse>> UpdateNotes(
        Guid id,
        UpdateDailyReportNotesRequest request,
        CancellationToken cancellationToken)
    {
        return await DraftAction(() => dailyReportService.UpdateNotesAsync(id, request, cancellationToken));
    }

    [HttpPut("{id:guid}/work-order")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<DailyReportResponse>> UpdateWorkOrder(
        Guid id,
        UpdateDailyReportWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        return await DraftAction(() => dailyReportService.UpdateWorkOrderAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/hours")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<DailyReportResponse>> AddWorkHours(
        Guid id,
        AddWorkHourRequest request,
        CancellationToken cancellationToken)
    {
        return await DraftAction(() => dailyReportService.AddWorkHoursAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/work")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<DailyReportResponse>> AddWork(
        Guid id,
        AddWorkEntryRequest request,
        CancellationToken cancellationToken)
    {
        return await DraftAction(() => dailyReportService.AddWorkAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/materials")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<DailyReportResponse>> AddMaterial(
        Guid id,
        AddMaterialUsageRequest request,
        CancellationToken cancellationToken)
    {
        return await DraftAction(() => dailyReportService.AddMaterialAsync(id, request, User.GetUserId(), cancellationToken));
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<DailyReportResponse>> Submit(Guid id, CancellationToken cancellationToken)
    {
        return await DraftAction(() => dailyReportService.SubmitDailyReportAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/comments")]
    [Authorize(Roles = "PM,SPM,Subcontractor,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<DailyReportResponse>> AddComment(
        Guid id,
        AddDailyReportCommentRequest request,
        CancellationToken cancellationToken)
    {
        return await ReviewAction(() => dailyReportService.AddCommentAsync(id, request, User.GetUserId(), cancellationToken));
    }

    [HttpPost("{id:guid}/comments/{commentId:guid}/resolve")]
    [Authorize(Roles = "PM,SPM,Subcontractor,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<DailyReportResponse>> ResolveComment(
        Guid id,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        return await ReviewAction(() => dailyReportService.ResolveCommentAsync(id, commentId, cancellationToken));
    }

    [HttpPost("{id:guid}/comments/{commentId:guid}/unresolve")]
    [Authorize(Roles = "PM,SPM,Subcontractor,Foreman,SubcontractorForeman")]
    public async Task<ActionResult<DailyReportResponse>> UnresolveComment(
        Guid id,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        return await ReviewAction(() => dailyReportService.UnresolveCommentAsync(id, commentId, cancellationToken));
    }

    [HttpPost("{id:guid}/send-back")]
    [Authorize(Roles = "PM,SPM")]
    public async Task<ActionResult<DailyReportResponse>> SendBack(Guid id, CancellationToken cancellationToken)
    {
        return await ReviewAction(() => dailyReportService.SendBackToForemanAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/pm-approve")]
    [Authorize(Roles = "PM")]
    public async Task<ActionResult<DailyReportResponse>> PmApprove(Guid id, CancellationToken cancellationToken)
    {
        return await ReviewAction(() => dailyReportService.PmApproveAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/spm-approve")]
    [Authorize(Roles = "SPM")]
    public async Task<ActionResult<DailyReportResponse>> SpmApprove(Guid id, CancellationToken cancellationToken)
    {
        return await ReviewAction(() => dailyReportService.SpmApproveAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/subcontractor-approve")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<ActionResult<DailyReportResponse>> SubcontractorApprove(Guid id, CancellationToken cancellationToken)
    {
        return await ReviewAction(() => dailyReportService.SubcontractorApproveAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "PM,SPM,Subcontractor")]
    public async Task<ActionResult<DailyReportResponse>> Reject(
        Guid id,
        RejectDailyReportRequest request,
        CancellationToken cancellationToken)
    {
        return await ReviewAction(() => dailyReportService.RejectDailyReportAsync(id, request, cancellationToken));
    }

    private async Task<ActionResult<DailyReportResponse>> DraftAction(Func<Task<DailyReportResponse>> action)
    {
        try
        {
            return Ok(await action());
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

    private async Task<ActionResult<DailyReportResponse>> ReviewAction(Func<Task<DailyReportResponse>> action)
    {
        try
        {
            return Ok(await action());
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

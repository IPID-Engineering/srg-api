using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Api.Extensions;
using SRG.Application.Analytics;

namespace SRG.Api.Controllers;

[ApiController]
[Route("analytics")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("pm")]
    [Authorize(Roles = "PM,SPM")]
    public async Task<ActionResult<PMAnalyticsResponse>> GetPMAnalytics(CancellationToken cancellationToken)
    {
        return Ok(await analyticsService.GetPMAnalyticsAsync(cancellationToken));
    }

    [HttpGet("logistics")]
    [Authorize(Roles = "Logistician")]
    public async Task<ActionResult<LogisticsAnalyticsResponse>> GetLogisticsAnalytics(
        [FromQuery] decimal lowStockThreshold = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await analyticsService.GetLogisticsAnalyticsAsync(lowStockThreshold, cancellationToken));
    }

    [HttpGet("foreman")]
    [Authorize(Roles = "Foreman,SubcontractorForeman")]
    public async Task<ActionResult<ForemanAnalyticsResponse>> GetForemanAnalytics(CancellationToken cancellationToken)
    {
        return Ok(await analyticsService.GetForemanAnalyticsAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("crew/{id:guid}")]
    [Authorize(Roles = "PM,SPM")]
    public async Task<ActionResult<CrewAnalyticsResponse>> GetCrewAnalytics(
        Guid id,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await analyticsService.GetCrewAnalyticsAsync(id, from, to, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("crew/{id:guid}/materials")]
    [Authorize(Roles = "PM,SPM")]
    public async Task<ActionResult<List<CrewMaterialUsageResponse>>> GetCrewMaterialUsage(
        Guid id,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await analyticsService.GetCrewMaterialUsageAsync(id, from, to, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("crew/{id:guid}/workers")]
    [Authorize(Roles = "PM,SPM")]
    public async Task<ActionResult<List<WorkerStatsResponse>>> GetCrewWorkerStats(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await analyticsService.GetWorkerStatsAsync(id, cancellationToken));
    }

    [HttpGet("material/{id:guid}")]
    [Authorize(Roles = "Logistician,PM,SPM")]
    public async Task<ActionResult<MaterialStatsResponse>> GetMaterialStats(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await analyticsService.GetMaterialStatsAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}

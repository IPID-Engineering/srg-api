using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Application.Analytics;

namespace SRG.Api.Controllers;

[ApiController]
[Route("crews/{crewId}/stats")]
[Authorize(Roles = "PM,SPM,Subcontractor,Foreman,SubcontractorForeman")]
public class CrewStatsController(ICrewStatsService crewStatsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CrewStatsResponse>> GetStats(
        Guid crewId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var request = new CrewStatsRequest(crewId, dateFrom, dateTo);
        var response = await crewStatsService.GetCrewStatsAsync(request, cancellationToken);
        return Ok(response);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Application.Audit;

namespace SRG.Api.Controllers;

[ApiController]
[Route("audit")]
[Authorize(Roles = "Admin,SPM")]
public class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AuditLogResponse>>> GetLogs(
        [FromQuery] Guid? userId,
        [FromQuery] string? entityName,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        return Ok(await auditService.GetLogsAsync(
            new AuditLogFilter(userId, entityName, action, from, to),
            cancellationToken));
    }
}

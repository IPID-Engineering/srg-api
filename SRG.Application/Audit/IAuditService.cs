namespace SRG.Application.Audit;

public interface IAuditService
{
    Task LogActionAsync(
        Guid userId,
        string action,
        string entityName,
        Guid entityId,
        object changes,
        CancellationToken cancellationToken = default);

    Task<List<AuditLogResponse>> GetLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
}

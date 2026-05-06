namespace SRG.Application.Audit;

public record AuditLogResponse(
    Guid Id,
    Guid UserId,
    string Action,
    string EntityName,
    Guid EntityId,
    string Changes,
    DateTime CreatedAt);

public record AuditLogFilter(
    Guid? UserId,
    string? EntityName,
    string? Action,
    DateTime? From,
    DateTime? To);

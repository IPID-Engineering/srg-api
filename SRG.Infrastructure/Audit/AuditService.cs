using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SRG.Application.Audit;
using SRG.Domain.Entities;
using SRG.Infrastructure.Persistence;

namespace SRG.Infrastructure.Audit;

public class AuditService(AppDbContext dbContext) : IAuditService
{
    public async Task LogActionAsync(
        Guid userId,
        string action,
        string entityName,
        Guid entityId,
        object changes,
        CancellationToken cancellationToken = default)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Changes = JsonSerializer.Serialize(changes),
            CreatedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AuditLogResponse>> GetLogsAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (filter.UserId is not null)
        {
            query = query.Where(log => log.UserId == filter.UserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
        {
            query = query.Where(log => log.EntityName == filter.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(log => log.Action == filter.Action);
        }

        if (filter.From is not null)
        {
            query = query.Where(log => log.CreatedAt >= filter.From);
        }

        if (filter.To is not null)
        {
            query = query.Where(log => log.CreatedAt <= filter.To);
        }

        return await query
            .OrderByDescending(log => log.CreatedAt)
            .Take(500)
            .Select(log => new AuditLogResponse(
                log.Id,
                log.UserId,
                log.Action,
                log.EntityName,
                log.EntityId,
                log.Changes,
                log.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

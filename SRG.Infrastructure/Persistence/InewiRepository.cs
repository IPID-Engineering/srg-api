using Microsoft.EntityFrameworkCore;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence;

public class InewiRepository(AppDbContext dbContext) : IInewiRepository
{
    public Task<List<InewiRecord>> GetBySubcontractorAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        return dbContext.InewiRecords
            .AsNoTracking()
            .Where(r => r.SubcontractorId == subcontractorId)
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.WorkerName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<InewiRecord>> GetByDateRangeAsync(Guid subcontractorId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return dbContext.InewiRecords
            .AsNoTracking()
            .Where(r => r.SubcontractorId == subcontractorId && r.Date >= from && r.Date <= to)
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.WorkerName)
            .ToListAsync(cancellationToken);
    }

    public Task<InewiRecord?> GetByWorkerAndDateAsync(Guid subcontractorId, string workerName, DateOnly date, CancellationToken cancellationToken = default)
    {
        return dbContext.InewiRecords
            .FirstOrDefaultAsync(r => r.SubcontractorId == subcontractorId && r.WorkerName == workerName && r.Date == date, cancellationToken);
    }

    public async Task AddAsync(InewiRecord record, CancellationToken cancellationToken = default)
    {
        await dbContext.InewiRecords.AddAsync(record, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<InewiIntegrationSettings?> GetIntegrationSettingsAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        return dbContext.InewiIntegrationSettings
            .FirstOrDefaultAsync(s => s.SubcontractorId == subcontractorId, cancellationToken);
    }

    public async Task AddIntegrationSettingsAsync(InewiIntegrationSettings settings, CancellationToken cancellationToken = default)
    {
        await dbContext.InewiIntegrationSettings.AddAsync(settings, cancellationToken);
    }

    public Task UpdateIntegrationSettingsAsync(InewiIntegrationSettings settings, CancellationToken cancellationToken = default)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        dbContext.InewiIntegrationSettings.Update(settings);
        return Task.CompletedTask;
    }

    public async Task DeleteIntegrationSettingsAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var settings = await GetIntegrationSettingsAsync(subcontractorId, cancellationToken);
        if (settings != null)
        {
            dbContext.InewiIntegrationSettings.Remove(settings);
        }
    }
}

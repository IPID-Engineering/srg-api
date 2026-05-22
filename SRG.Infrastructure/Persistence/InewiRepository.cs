using Microsoft.EntityFrameworkCore;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence;

public class InewiRepository(AppDbContext dbContext) : IInewiRepository
{
    public Task<List<InewiRecord>> GetBySubcontractorCrewAsync(Guid subcontractorCrewId, CancellationToken cancellationToken = default)
    {
        return dbContext.InewiRecords
            .AsNoTracking()
            .Where(r => r.SubcontractorCrewId == subcontractorCrewId)
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.WorkerName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<InewiRecord>> GetByDateRangeAsync(Guid subcontractorCrewId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return dbContext.InewiRecords
            .AsNoTracking()
            .Where(r => r.SubcontractorCrewId == subcontractorCrewId && r.Date >= from && r.Date <= to)
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.WorkerName)
            .ToListAsync(cancellationToken);
    }

    public Task<InewiRecord?> GetByWorkerAndDateAsync(Guid subcontractorCrewId, string workerName, DateOnly date, CancellationToken cancellationToken = default)
    {
        return dbContext.InewiRecords
            .FirstOrDefaultAsync(r => r.SubcontractorCrewId == subcontractorCrewId && r.WorkerName == workerName && r.Date == date, cancellationToken);
    }

    public async Task AddAsync(InewiRecord record, CancellationToken cancellationToken = default)
    {
        await dbContext.InewiRecords.AddAsync(record, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

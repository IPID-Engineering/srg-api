using Microsoft.EntityFrameworkCore;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence;

public class RateGroupRepository(AppDbContext dbContext) : IRateGroupRepository
{
    public Task<List<RateGroup>> GetAllBySubcontractorAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        return dbContext.RateGroups
            .Include(rg => rg.Workers)
            .Where(rg => rg.SubcontractorId == subcontractorId)
            .OrderBy(rg => rg.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<RateGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.RateGroups
            .Include(rg => rg.Workers)
            .FirstOrDefaultAsync(rg => rg.Id == id, cancellationToken);
    }

    public async Task AddAsync(RateGroup rateGroup, CancellationToken cancellationToken = default)
    {
        await dbContext.RateGroups.AddAsync(rateGroup, cancellationToken);
    }

    public Task DeleteAsync(RateGroup rateGroup, CancellationToken cancellationToken = default)
    {
        dbContext.RateGroups.Remove(rateGroup);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

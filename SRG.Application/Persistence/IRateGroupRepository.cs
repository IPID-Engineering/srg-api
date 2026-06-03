using SRG.Domain.Entities;

namespace SRG.Application.Persistence;

public interface IRateGroupRepository
{
    Task<List<RateGroup>> GetAllBySubcontractorAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<RateGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(RateGroup rateGroup, CancellationToken cancellationToken = default);
    Task DeleteAsync(RateGroup rateGroup, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

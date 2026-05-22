using SRG.Domain.Entities;

namespace SRG.Application.Persistence;

public interface IInewiRepository
{
    Task<List<InewiRecord>> GetBySubcontractorCrewAsync(Guid subcontractorCrewId, CancellationToken cancellationToken = default);
    Task<List<InewiRecord>> GetByDateRangeAsync(Guid subcontractorCrewId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<InewiRecord?> GetByWorkerAndDateAsync(Guid subcontractorCrewId, string workerName, DateOnly date, CancellationToken cancellationToken = default);
    Task AddAsync(InewiRecord record, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

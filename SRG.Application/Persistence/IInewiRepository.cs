using SRG.Domain.Entities;

namespace SRG.Application.Persistence;

public interface IInewiRepository
{
    Task<List<InewiRecord>> GetBySubcontractorAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<List<InewiRecord>> GetByDateRangeAsync(Guid subcontractorId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<InewiRecord?> GetByWorkerAndDateAsync(Guid subcontractorId, string workerName, DateOnly date, CancellationToken cancellationToken = default);
    Task AddAsync(InewiRecord record, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    
    // Integration settings
    Task<InewiIntegrationSettings?> GetIntegrationSettingsAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task AddIntegrationSettingsAsync(InewiIntegrationSettings settings, CancellationToken cancellationToken = default);
    Task UpdateIntegrationSettingsAsync(InewiIntegrationSettings settings, CancellationToken cancellationToken = default);
    Task DeleteIntegrationSettingsAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
}

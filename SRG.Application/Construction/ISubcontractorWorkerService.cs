namespace SRG.Application.Construction;

public interface ISubcontractorWorkerService
{
    Task<SubcontractorWorkerResponse> CreateAsync(CreateSubcontractorWorkerRequest request, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<SubcontractorWorkerResponse> UpdateAsync(Guid id, UpdateSubcontractorWorkerRequest request, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<List<SubcontractorWorkerResponse>> GetMineAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<List<SubcontractorWorkerResponse>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<DeleteWorkerImpactResponse> GetDeleteWorkerImpactAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default);
}

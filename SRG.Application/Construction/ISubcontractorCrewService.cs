namespace SRG.Application.Construction;

public interface ISubcontractorCrewService
{
    Task<SubcontractorCrewResponse> CreateAsync(CreateSubcontractorCrewRequest request, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<List<SubcontractorCrewResponse>> GetMyCrewsAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<SubcontractorCrewDetailResponse> GetByIdAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<SubcontractorCrewResponse> UpdateAsync(Guid id, UpdateSubcontractorCrewRequest request, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task SetForemanAsync(Guid crewId, SetForemanRequest request, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task AssignWorkerToCrewAsync(Guid crewId, Guid workerId, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task RemoveWorkerFromCrewAsync(Guid crewId, Guid workerId, Guid subcontractorId, CancellationToken cancellationToken = default);
    
    // PM Access Management - managed by Subcontractor
    Task<SubcontractorCrewWithPmAccessResponse> GetCrewWithPmAccessAsync(Guid crewId, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<SubcontractorCrewPmAccessResponse> GrantPmAccessAsync(Guid crewId, Guid pmUserId, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task RevokePmAccessAsync(Guid crewId, Guid pmUserId, Guid subcontractorId, CancellationToken cancellationToken = default);
    
    // For PM - get crews they have access to
    Task<List<SubcontractorCrewResponse>> GetCrewsForPmAsync(Guid pmUserId, CancellationToken cancellationToken = default);
}

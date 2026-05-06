namespace SRG.Application.Construction;

public interface IProjectSubcontractorService
{
    Task<ProjectSubcontractorResponse> AssignAsync(Guid projectId, AssignSubcontractorRequest request, CancellationToken cancellationToken = default);
    Task<List<ProjectSubcontractorResponse>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}

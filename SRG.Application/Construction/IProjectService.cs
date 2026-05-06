namespace SRG.Application.Construction;

public interface IProjectService
{
    Task<ProjectResponse> CreateProjectAsync(
        CreateProjectRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default);

    Task<List<ProjectResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

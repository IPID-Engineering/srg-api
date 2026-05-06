namespace SRG.Application.Construction;

public interface ICrewService
{
    Task<CrewResponse> CreateCrewAsync(
        CreateCrewRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default);

    Task<CrewResponse> AssignToProjectAsync(
        Guid crewId,
        AssignCrewRequest request,
        CancellationToken cancellationToken = default);

    Task<List<CrewResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<CrewResponse>> GetByCreatorAsync(Guid creatorId, CancellationToken cancellationToken = default);
    Task<List<CrewResponse>> GetByUserAccessAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CrewResponse>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}

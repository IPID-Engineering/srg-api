namespace SRG.Application.Construction;

public interface ITeamService
{
    Task<TeamResponse> CreateTeamAsync(CreateTeamRequest request, CancellationToken cancellationToken = default);
    Task<List<TeamResponse>> GetByCrewAsync(Guid crewId, CancellationToken cancellationToken = default);
}

using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Construction;

public class TeamService(IConstructionRepository construction) : ITeamService
{
    public async Task<TeamResponse> CreateTeamAsync(
        CreateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Team name is required.");
        }

        _ = await construction.GetCrewByIdAsync(request.CrewId, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        var team = new Team
        {
            Name = request.Name.Trim(),
            CrewId = request.CrewId,
        };

        await construction.AddTeamAsync(team, cancellationToken);
        await construction.SaveChangesAsync(cancellationToken);

        return ToResponse(team);
    }

    public async Task<List<TeamResponse>> GetByCrewAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        _ = await construction.GetCrewByIdAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        var teams = await construction.GetTeamsByCrewAsync(crewId, cancellationToken);
        return teams.Select(ToResponse).ToList();
    }

    private static TeamResponse ToResponse(Team team)
    {
        return new TeamResponse(team.Id, team.Name, team.CrewId);
    }
}

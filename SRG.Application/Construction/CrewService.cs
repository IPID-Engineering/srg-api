using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Construction;

public class CrewService(IConstructionRepository construction) : ICrewService
{
    public async Task<CrewResponse> CreateCrewAsync(
        CreateCrewRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Crew name is required.");
        }

        _ = await construction.GetProjectByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        var crew = new Crew
        {
            Name = request.Name.Trim(),
            ProjectId = request.ProjectId,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow,
        };

        await construction.AddCrewAsync(crew, cancellationToken);
        await construction.SaveChangesAsync(cancellationToken);

        return ToResponse(crew);
    }

    public async Task<CrewResponse> AssignToProjectAsync(
        Guid crewId,
        AssignCrewRequest request,
        CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetCrewByIdAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        _ = await construction.GetProjectByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        crew.ProjectId = request.ProjectId;
        await construction.SaveChangesAsync(cancellationToken);

        return ToResponse(crew);
    }

    public async Task<List<CrewResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var crews = await construction.GetAllCrewsAsync(cancellationToken);
        return crews.Select(ToResponse).ToList();
    }

    public async Task<List<CrewResponse>> GetByCreatorAsync(Guid creatorId, CancellationToken cancellationToken = default)
    {
        var crews = await construction.GetCrewsByCreatorAsync(creatorId, cancellationToken);
        return crews.Select(ToResponse).ToList();
    }

    public async Task<List<CrewResponse>> GetByUserAccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var crews = await construction.GetCrewsByUserAccessAsync(userId, cancellationToken);
        return crews.Select(ToResponse).ToList();
    }

    public async Task<List<CrewResponse>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        _ = await construction.GetProjectByIdAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        var crews = await construction.GetCrewsByProjectAsync(projectId, cancellationToken);
        return crews.Select(ToResponse).ToList();
    }

    private static CrewResponse ToResponse(Crew crew)
    {
        return new CrewResponse(
            crew.Id,
            crew.Name,
            crew.ProjectId,
            crew.CreatedById,
            crew.CreatedAt);
    }
}

using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Construction;

public class ProjectService(IConstructionRepository construction) : IProjectService
{
    public async Task<ProjectResponse> CreateProjectAsync(
        CreateProjectRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name, "Project name is required.");

        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow,
        };

        await construction.AddProjectAsync(project, cancellationToken);
        await construction.SaveChangesAsync(cancellationToken);

        return ToResponse(project);
    }

    public async Task<List<ProjectResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var projects = await construction.GetProjectsAsync(cancellationToken);
        return projects.Select(ToResponse).ToList();
    }

    public async Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await construction.GetProjectByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        return ToResponse(project);
    }

    private static void ValidateName(string name, string message)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(message);
        }
    }

    private static ProjectResponse ToResponse(Project project)
    {
        return new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.CreatedById,
            project.CreatedAt);
    }
}

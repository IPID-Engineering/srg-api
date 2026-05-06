using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.Construction;

public class ProjectSubcontractorService(
    IConstructionRepository construction,
    IUserRepository users) : IProjectSubcontractorService
{
    public async Task<ProjectSubcontractorResponse> AssignAsync(
        Guid projectId,
        AssignSubcontractorRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await construction.GetProjectByIdAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        var subcontractor = await users.GetByIdAsync(request.SubcontractorId, cancellationToken)
            ?? throw new KeyNotFoundException("Subcontractor was not found.");

        if (subcontractor.Role != UserRole.Subcontractor)
        {
            throw new ValidationException("Assigned user must have Subcontractor role.");
        }

        var existing = await construction.GetProjectSubcontractorAsync(projectId, request.SubcontractorId, cancellationToken);

        if (existing is not null)
        {
            return ToResponse(existing);
        }

        var assignment = new ProjectSubcontractor
        {
            ProjectId = projectId,
            SubcontractorId = request.SubcontractorId,
        };

        await construction.AddProjectSubcontractorAsync(assignment, cancellationToken);
        await construction.SaveChangesAsync(cancellationToken);

        return ToResponse(assignment, subcontractor.Email);
    }

    public async Task<List<ProjectSubcontractorResponse>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        _ = await construction.GetProjectByIdAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        var assignments = await construction.GetProjectSubcontractorsAsync(projectId, cancellationToken);
        return assignments.Select(assignment => ToResponse(assignment)).ToList();
    }

    private static ProjectSubcontractorResponse ToResponse(ProjectSubcontractor assignment, string? email = null)
    {
        return new ProjectSubcontractorResponse(
            assignment.Id,
            assignment.ProjectId,
            assignment.SubcontractorId,
            email ?? assignment.Subcontractor?.Email);
    }
}

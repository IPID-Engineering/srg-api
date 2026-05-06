using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Construction;

public class SectionService(IConstructionRepository construction) : ISectionService
{
    public async Task<SectionResponse> CreateSectionAsync(
        CreateSectionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Section name is required.");
        }

        _ = await construction.GetProjectByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        var section = new Section
        {
            Name = request.Name.Trim(),
            ProjectId = request.ProjectId,
        };

        await construction.AddSectionAsync(section, cancellationToken);
        await construction.SaveChangesAsync(cancellationToken);

        return ToResponse(section);
    }

    public async Task<List<SectionResponse>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        _ = await construction.GetProjectByIdAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        var sections = await construction.GetSectionsByProjectAsync(projectId, cancellationToken);
        return sections.Select(ToResponse).ToList();
    }

    public async Task<InstallationResponse> CreateInstallationAsync(
        CreateInstallationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Installation name is required.");
        }

        _ = await construction.GetSectionByIdAsync(request.SectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Section was not found.");

        var installation = new Installation
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            SectionId = request.SectionId,
        };

        await construction.AddInstallationAsync(installation, cancellationToken);
        await construction.SaveChangesAsync(cancellationToken);

        return ToInstallationResponse(installation);
    }

    public async Task<List<InstallationResponse>> GetInstallationsBySectionAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        _ = await construction.GetSectionByIdAsync(sectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Section was not found.");

        var installations = await construction.GetInstallationsBySectionAsync(sectionId, cancellationToken);
        return installations.Select(ToInstallationResponse).ToList();
    }

    private static SectionResponse ToResponse(Section section)
    {
        return new SectionResponse(
            section.Id, 
            section.Name, 
            section.ProjectId,
            section.Installations.Select(ToInstallationResponse).ToList());
    }

    private static InstallationResponse ToInstallationResponse(Installation installation)
    {
        return new InstallationResponse(
            installation.Id, 
            installation.Name, 
            installation.Description, 
            installation.SectionId, 
            installation.CreatedAt);
    }
}

namespace SRG.Application.Construction;

public interface ISectionService
{
    Task<SectionResponse> CreateSectionAsync(CreateSectionRequest request, CancellationToken cancellationToken = default);
    Task<List<SectionResponse>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<InstallationResponse> CreateInstallationAsync(CreateInstallationRequest request, CancellationToken cancellationToken = default);
    Task<List<InstallationResponse>> GetInstallationsBySectionAsync(Guid sectionId, CancellationToken cancellationToken = default);
}

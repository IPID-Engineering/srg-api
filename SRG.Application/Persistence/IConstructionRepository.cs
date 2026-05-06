using SRG.Domain.Entities;

namespace SRG.Application.Persistence;

public interface IConstructionRepository
{
    Task<List<Project>> GetProjectsAsync(CancellationToken cancellationToken = default);
    Task<Project?> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddProjectAsync(Project project, CancellationToken cancellationToken = default);

    Task<List<Section>> GetSectionsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Section?> GetSectionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddSectionAsync(Section section, CancellationToken cancellationToken = default);

    Task<List<Installation>> GetInstallationsBySectionAsync(Guid sectionId, CancellationToken cancellationToken = default);
    Task AddInstallationAsync(Installation installation, CancellationToken cancellationToken = default);

    Task<List<Crew>> GetAllCrewsAsync(CancellationToken cancellationToken = default);
    Task<List<Crew>> GetCrewsByCreatorAsync(Guid creatorId, CancellationToken cancellationToken = default);
    Task<List<Crew>> GetCrewsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Crew?> GetCrewByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddCrewAsync(Crew crew, CancellationToken cancellationToken = default);

    Task<List<Team>> GetTeamsByCrewAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task<Team?> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddTeamAsync(Team team, CancellationToken cancellationToken = default);

    Task<List<Worker>> GetWorkerByCrewAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task<Worker?> GetWorkerByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Worker>> GetWorkersByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddPersonAsync(Worker person, CancellationToken cancellationToken = default);
    void RemovePerson(Worker person);

    Task<List<SubcontractorWorker>> GetSubcontractorWorkersAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<SubcontractorWorker?> GetSubcontractorWorkerByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SubcontractorWorker?> GetSubcontractorWorkerByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<SubcontractorWorker>> GetSubcontractorWorkersByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<List<SubcontractorWorker>> GetSubcontractorWorkersByCrewAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task AddSubcontractorWorkerAsync(SubcontractorWorker worker, CancellationToken cancellationToken = default);
    void RemoveSubcontractorWorker(SubcontractorWorker worker);

    Task<List<SubcontractorCrew>> GetSubcontractorCrewsAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<List<SubcontractorCrew>> GetAllSubcontractorCrewsAsync(CancellationToken cancellationToken = default);
    Task<SubcontractorCrew?> GetSubcontractorCrewByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SubcontractorCrew?> GetSubcontractorCrewWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddSubcontractorCrewAsync(SubcontractorCrew crew, CancellationToken cancellationToken = default);
    void RemoveSubcontractorCrew(SubcontractorCrew crew);

    Task<List<SubcontractorForemanHistory>> GetForemanHistoryByCrewAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task<SubcontractorForemanHistory?> GetCurrentForemanHistoryAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task AddForemanHistoryAsync(SubcontractorForemanHistory history, CancellationToken cancellationToken = default);

    Task<ProjectSubcontractor?> GetProjectSubcontractorAsync(Guid projectId, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<List<ProjectSubcontractor>> GetProjectSubcontractorsAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddProjectSubcontractorAsync(ProjectSubcontractor assignment, CancellationToken cancellationToken = default);

    // Crew Access Management (old system - kept for backwards compatibility)
    Task<List<Crew>> GetCrewsWithAccessAsync(CancellationToken cancellationToken = default);
    Task<Crew?> GetCrewWithAccessAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task<List<Crew>> GetCrewsByUserAccessAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CrewAccess?> GetCrewAccessAsync(Guid crewId, Guid userId, CancellationToken cancellationToken = default);
    Task AddCrewAccessAsync(CrewAccess access, CancellationToken cancellationToken = default);
    void RemoveCrewAccess(CrewAccess access);

    // SubcontractorCrew PM Access Management (new system)
    Task<SubcontractorCrew?> GetSubcontractorCrewWithPmAccessAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task<List<SubcontractorCrew>> GetSubcontractorCrewsByPmAccessAsync(Guid pmUserId, CancellationToken cancellationToken = default);
    Task<SubcontractorCrewPmAccess?> GetSubcontractorCrewPmAccessAsync(Guid crewId, Guid pmUserId, CancellationToken cancellationToken = default);
    Task AddSubcontractorCrewPmAccessAsync(SubcontractorCrewPmAccess access, CancellationToken cancellationToken = default);
    void RemoveSubcontractorCrewPmAccess(SubcontractorCrewPmAccess access);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

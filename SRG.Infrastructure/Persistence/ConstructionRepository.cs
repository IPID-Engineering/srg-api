using Microsoft.EntityFrameworkCore;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence;

public class ConstructionRepository(AppDbContext dbContext) : IConstructionRepository
{
    public Task<List<Project>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Projects
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Project?> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Projects
            .Include(project => project.Sections)
            .Include(project => project.Crews)
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
    }

    public async Task AddProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        await dbContext.Projects.AddAsync(project, cancellationToken);
    }

    public Task<List<Section>> GetSectionsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return dbContext.Sections
            .Include(section => section.Installations)
            .Where(section => section.ProjectId == projectId)
            .OrderBy(section => section.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Section?> GetSectionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Sections
            .Include(section => section.Installations)
            .FirstOrDefaultAsync(section => section.Id == id, cancellationToken);
    }

    public async Task AddSectionAsync(Section section, CancellationToken cancellationToken = default)
    {
        await dbContext.Sections.AddAsync(section, cancellationToken);
    }

    public Task<List<Installation>> GetInstallationsBySectionAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        return dbContext.Installations
            .Where(installation => installation.SectionId == sectionId)
            .OrderBy(installation => installation.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddInstallationAsync(Installation installation, CancellationToken cancellationToken = default)
    {
        await dbContext.Installations.AddAsync(installation, cancellationToken);
    }

    public Task<List<Crew>> GetAllCrewsAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Crews
            .OrderBy(crew => crew.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Crew>> GetCrewsByCreatorAsync(Guid creatorId, CancellationToken cancellationToken = default)
    {
        return dbContext.Crews
            .Where(crew => crew.CreatedById == creatorId)
            .OrderBy(crew => crew.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Crew>> GetCrewsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return dbContext.Crews
            .Where(crew => crew.ProjectId == projectId)
            .OrderBy(crew => crew.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Crew?> GetCrewByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Crews.FirstOrDefaultAsync(crew => crew.Id == id, cancellationToken);
    }

    public async Task AddCrewAsync(Crew crew, CancellationToken cancellationToken = default)
    {
        await dbContext.Crews.AddAsync(crew, cancellationToken);
    }

    public Task<List<Team>> GetTeamsByCrewAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        return dbContext.Teams
            .Where(team => team.CrewId == crewId)
            .OrderBy(team => team.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Team?> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Teams.FirstOrDefaultAsync(team => team.Id == id, cancellationToken);
    }

    public async Task AddTeamAsync(Team team, CancellationToken cancellationToken = default)
    {
        await dbContext.Teams.AddAsync(team, cancellationToken);
    }

    public Task<List<Worker>> GetWorkerByCrewAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        return dbContext.Workers
            .Where(worker => worker.CrewId == crewId)
            .OrderBy(worker => worker.LastName)
            .ThenBy(worker => worker.FirstName)
            .ToListAsync(cancellationToken);
    }

    public Task<Worker?> GetWorkerByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Workers.FirstOrDefaultAsync(worker => worker.Id == id, cancellationToken);
    }

    public Task<List<Worker>> GetWorkersByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return dbContext.Workers
            .Where(worker => worker.Crew!.ProjectId == projectId)
            .OrderBy(worker => worker.LastName)
            .ThenBy(worker => worker.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddPersonAsync(Worker worker, CancellationToken cancellationToken = default)
    {
        await dbContext.Workers.AddAsync(worker, cancellationToken);
    }

    public void RemovePerson(Worker worker)
    {
        dbContext.Workers.Remove(worker);
    }

    public Task<List<SubcontractorWorker>> GetSubcontractorWorkersAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorWorkers
            .Where(worker => worker.SubcontractorId == subcontractorId && worker.CrewId != null)
            .OrderBy(worker => worker.LastName)
            .ThenBy(worker => worker.FirstName)
            .ToListAsync(cancellationToken);
    }

    public Task<SubcontractorWorker?> GetSubcontractorWorkerByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorWorkers.FirstOrDefaultAsync(worker => worker.Id == id, cancellationToken);
    }

    public Task<SubcontractorWorker?> GetSubcontractorWorkerByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorWorkers
            .Include(worker => worker.Crew)
            .FirstOrDefaultAsync(worker => worker.Email != null && worker.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public Task<List<SubcontractorWorker>> GetSubcontractorWorkersByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return dbContext.ProjectSubcontractors
            .Where(assignment => assignment.ProjectId == projectId)
            .Join(
                dbContext.SubcontractorWorkers,
                assignment => assignment.SubcontractorId,
                worker => worker.SubcontractorId,
                (_, worker) => worker)
            .OrderBy(worker => worker.LastName)
            .ThenBy(worker => worker.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddSubcontractorWorkerAsync(SubcontractorWorker worker, CancellationToken cancellationToken = default)
    {
        await dbContext.SubcontractorWorkers.AddAsync(worker, cancellationToken);
    }

    public void RemoveSubcontractorWorker(SubcontractorWorker worker)
    {
        dbContext.SubcontractorWorkers.Remove(worker);
    }

    public async Task<int> ClearInewiMappingsForOrphanedWorkersAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        return await dbContext.SubcontractorWorkers
            .Where(w => w.SubcontractorId == subcontractorId && w.CrewId == null && w.InewiEmployeeId != null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(w => w.InewiEmployeeId, (string?)null), cancellationToken);
    }

    public Task<List<SubcontractorWorker>> GetSubcontractorWorkersByCrewAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorWorkers
            .Where(worker => worker.CrewId == crewId)
            .OrderBy(worker => worker.LastName)
            .ThenBy(worker => worker.FirstName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<SubcontractorCrew>> GetSubcontractorCrewsAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorCrews
            .Include(crew => crew.CurrentForeman)
            .Include(crew => crew.Workers)
            .Where(crew => crew.SubcontractorId == subcontractorId)
            .OrderBy(crew => crew.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<SubcontractorCrew>> GetAllSubcontractorCrewsAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorCrews
            .Include(crew => crew.CurrentForeman)
            .OrderBy(crew => crew.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<SubcontractorCrew?> GetSubcontractorCrewByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorCrews
            .Include(crew => crew.CurrentForeman)
            .FirstOrDefaultAsync(crew => crew.Id == id, cancellationToken);
    }

    public Task<SubcontractorCrew?> GetSubcontractorCrewWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorCrews
            .Include(crew => crew.CurrentForeman)
            .Include(crew => crew.Workers)
            .Include(crew => crew.ForemanHistory)
                .ThenInclude(history => history.Foreman)
            .FirstOrDefaultAsync(crew => crew.Id == id, cancellationToken);
    }

    public async Task AddSubcontractorCrewAsync(SubcontractorCrew crew, CancellationToken cancellationToken = default)
    {
        await dbContext.SubcontractorCrews.AddAsync(crew, cancellationToken);
    }

    public void RemoveSubcontractorCrew(SubcontractorCrew crew)
    {
        dbContext.SubcontractorCrews.Remove(crew);
    }

    public Task<List<SubcontractorForemanHistory>> GetForemanHistoryByCrewAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorForemanHistory
            .Include(history => history.Foreman)
            .Where(history => history.CrewId == crewId)
            .OrderByDescending(history => history.StartDate)
            .ToListAsync(cancellationToken);
    }

    public Task<SubcontractorForemanHistory?> GetCurrentForemanHistoryAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorForemanHistory
            .FirstOrDefaultAsync(history => history.CrewId == crewId && history.EndDate == null, cancellationToken);
    }

    public async Task AddForemanHistoryAsync(SubcontractorForemanHistory history, CancellationToken cancellationToken = default)
    {
        await dbContext.SubcontractorForemanHistory.AddAsync(history, cancellationToken);
    }

    public Task<ProjectSubcontractor?> GetProjectSubcontractorAsync(Guid projectId, Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        return dbContext.ProjectSubcontractors
            .FirstOrDefaultAsync(
                assignment => assignment.ProjectId == projectId && assignment.SubcontractorId == subcontractorId,
                cancellationToken);
    }

    public Task<List<ProjectSubcontractor>> GetProjectSubcontractorsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return dbContext.ProjectSubcontractors
            .Include(assignment => assignment.Subcontractor)
            .Where(assignment => assignment.ProjectId == projectId)
            .OrderBy(assignment => assignment.Subcontractor!.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task AddProjectSubcontractorAsync(ProjectSubcontractor assignment, CancellationToken cancellationToken = default)
    {
        await dbContext.ProjectSubcontractors.AddAsync(assignment, cancellationToken);
    }

    // Crew Access Management
    public Task<List<Crew>> GetCrewsWithAccessAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Crews
            .Include(c => c.Project)
            .Include(c => c.CreatedBy)
            .Include(c => c.AccessList)
                .ThenInclude(a => a.User)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Crew?> GetCrewWithAccessAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        return dbContext.Crews
            .Include(c => c.Project)
            .Include(c => c.CreatedBy)
            .Include(c => c.AccessList)
                .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(c => c.Id == crewId, cancellationToken);
    }

    public Task<List<Crew>> GetCrewsByUserAccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Crews
            .Include(c => c.Project)
            .Where(c => c.AccessList.Any(a => a.UserId == userId))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<CrewAccess?> GetCrewAccessAsync(Guid crewId, Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.CrewAccessList
            .FirstOrDefaultAsync(a => a.CrewId == crewId && a.UserId == userId, cancellationToken);
    }

    public async Task AddCrewAccessAsync(CrewAccess access, CancellationToken cancellationToken = default)
    {
        await dbContext.CrewAccessList.AddAsync(access, cancellationToken);
    }

    public void RemoveCrewAccess(CrewAccess access)
    {
        dbContext.CrewAccessList.Remove(access);
    }

    // SubcontractorCrew PM Access Management
    public Task<SubcontractorCrew?> GetSubcontractorCrewWithPmAccessAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorCrews
            .Include(c => c.CurrentForeman)
            .Include(c => c.Workers)
            .Include(c => c.PmAccessList)
                .ThenInclude(a => a.PmUser)
            .FirstOrDefaultAsync(c => c.Id == crewId, cancellationToken);
    }

    public Task<List<SubcontractorCrew>> GetSubcontractorCrewsByPmAccessAsync(Guid pmUserId, CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorCrews
            .Include(c => c.CurrentForeman)
            .Include(c => c.Workers)
            .Where(c => c.PmAccessList.Any(a => a.PmUserId == pmUserId))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<SubcontractorCrewPmAccess?> GetSubcontractorCrewPmAccessAsync(Guid crewId, Guid pmUserId, CancellationToken cancellationToken = default)
    {
        return dbContext.SubcontractorCrewPmAccessList
            .FirstOrDefaultAsync(a => a.CrewId == crewId && a.PmUserId == pmUserId, cancellationToken);
    }

    public async Task AddSubcontractorCrewPmAccessAsync(SubcontractorCrewPmAccess access, CancellationToken cancellationToken = default)
    {
        await dbContext.SubcontractorCrewPmAccessList.AddAsync(access, cancellationToken);
    }

    public void RemoveSubcontractorCrewPmAccess(SubcontractorCrewPmAccess access)
    {
        dbContext.SubcontractorCrewPmAccessList.Remove(access);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Construction;

public class WorkerService(IConstructionRepository construction) : IWorkerService
{
    public async Task<WorkerResponse> AddPersonAsync(
        AddPersonRequest request,
        Guid foremanId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName)
            || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ValidationException("First name and last name are required.");
        }

        var crew = await construction.GetCrewByIdAsync(request.CrewId, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        if (crew.CreatedById != foremanId)
        {
            throw new ValidationException("Foreman can add workers only to crews they created.");
        }

        if (request.TeamId is not null)
        {
            var team = await construction.GetTeamByIdAsync(request.TeamId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Team was not found.");

            if (team.CrewId != request.CrewId)
            {
                throw new ValidationException("Team must belong to the selected Crew.");
            }
        }

        var person = new Worker
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CrewId = request.CrewId,
            CreatedById = foremanId,
            CreatedAt = DateTime.UtcNow,
            TeamId = request.TeamId,
        };

        await construction.AddPersonAsync(person, cancellationToken);
        await construction.SaveChangesAsync(cancellationToken);

        return ToResponse(person);
    }

    public async Task<List<WorkerResponse>> GetByCrewAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        _ = await construction.GetCrewByIdAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        var worker = await construction.GetWorkerByCrewAsync(crewId, cancellationToken);
        return worker.Select(ToResponse).ToList();
    }

    public async Task RemovePersonAsync(Guid id, Guid foremanId, CancellationToken cancellationToken = default)
    {
        var person = await construction.GetWorkerByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Worker was not found.");

        if (person.CreatedById != foremanId)
        {
            throw new ValidationException("Foreman can remove only their own crew workers.");
        }

        construction.RemovePerson(person);
        await construction.SaveChangesAsync(cancellationToken);
    }

    private static WorkerResponse ToResponse(Worker person)
    {
        return new WorkerResponse(
            person.Id,
            person.FirstName,
            person.LastName,
            person.CrewId,
            person.TeamId,
            person.CreatedById,
            person.CreatedAt);
    }
}

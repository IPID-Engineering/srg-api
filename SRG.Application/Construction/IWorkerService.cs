namespace SRG.Application.Construction;

public interface IWorkerService
{
    Task<WorkerResponse> AddPersonAsync(AddPersonRequest request, Guid foremanId, CancellationToken cancellationToken = default);
    Task<List<WorkerResponse>> GetByCrewAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task RemovePersonAsync(Guid id, Guid foremanId, CancellationToken cancellationToken = default);
}

namespace SRG.Application.WorkOrders;

public interface IWorkTypeService
{
    Task<List<WorkTypeResponse>> GetWorkTypesAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<WorkTypeResponse> CreateWorkTypeAsync(WorkTypeRequest request, CancellationToken cancellationToken = default);
    Task<WorkTypeResponse> UpdateWorkTypeAsync(Guid id, WorkTypeRequest request, CancellationToken cancellationToken = default);
    Task<WorkTypeResponse> DeactivateWorkTypeAsync(Guid id, CancellationToken cancellationToken = default);
}

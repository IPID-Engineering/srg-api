namespace SRG.Application.WorkOrders;

public interface IWorkOrderService
{
    Task<List<WorkOrderResponse>> GetWorkOrdersAsync(Guid userId, string role, Guid? crewId, CancellationToken cancellationToken = default);
    Task<WorkOrderResponse> GetWorkOrderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkOrderResponse> CreateWorkOrderAsync(CreateWorkOrderRequest request, Guid createdById, CancellationToken cancellationToken = default);
    Task<WorkOrderResponse> UpdateWorkOrderAsync(Guid id, UpdateWorkOrderRequest request, CancellationToken cancellationToken = default);
    Task DeleteWorkOrderAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<DeleteWorkOrderImpactResponse> GetDeleteWorkOrderImpactAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkOrderResponse> AddOrderedWorkAsync(Guid id, AddOrderedWorkRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<WorkOrderResponse> AddOrderedMaterialAsync(Guid id, AddOrderedMaterialRequest request, Guid userId, string userRole, CancellationToken cancellationToken = default);
    Task RemoveOrderedWorkAsync(Guid workOrderId, Guid orderedWorkId, Guid userId, CancellationToken cancellationToken = default);
    Task RemoveOrderedMaterialAsync(Guid workOrderId, Guid orderedMaterialId, Guid userId, CancellationToken cancellationToken = default);
    Task<WorkOrderProgressResponse> GetProgressAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkOrderResponse> AcceptWorkOrderAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}

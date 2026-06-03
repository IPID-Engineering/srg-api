using SRG.Domain.Entities;

namespace SRG.Application.Persistence;

public interface IWorkOrderRepository
{
    Task<List<WorkType>> GetWorkTypesAsync(CancellationToken cancellationToken = default);
    Task<WorkType?> GetWorkTypeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkType?> GetWorkTypeByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddWorkTypeAsync(WorkType workType, CancellationToken cancellationToken = default);
    void RemoveWorkType(WorkType workType);

    Task<List<WorkOrder>> GetWorkOrdersAsync(CancellationToken cancellationToken = default);
    Task<List<WorkOrder>> GetWorkOrdersForCrewAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task<List<WorkOrder>> GetWorkOrdersForSubcontractorAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetWorkOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetWorkOrderByNumberAsync(string number, CancellationToken cancellationToken = default);
    Task<int> GetNextWorkOrderSequenceAsync(CancellationToken cancellationToken = default);
    Task AddWorkOrderAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);
    void RemoveWorkOrder(WorkOrder workOrder);
    Task AddOrderedWorkAsync(OrderedWork orderedWork, CancellationToken cancellationToken = default);
    Task AddOrderedMaterialAsync(OrderedMaterial orderedMaterial, CancellationToken cancellationToken = default);
    void RemoveOrderedWork(OrderedWork orderedWork);
    void RemoveOrderedMaterial(OrderedMaterial orderedMaterial);
    Task<OrderedWork?> GetOrderedWorkByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrderedMaterial?> GetOrderedMaterialByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task ClearSubcontractorCrewFromWorkOrdersAsync(Guid subcontractorCrewId, CancellationToken cancellationToken = default);
    Task<int> CountWorkOrdersBySubcontractorCrewAsync(Guid subcontractorCrewId, CancellationToken cancellationToken = default);
    Task ClearWorkOrderFromDailyReportsAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<int> CountDailyReportsByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<int> CountIssuesByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<bool> HasConfirmedIssuesForWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<int> CountMaterialRequestsByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task RemoveMaterialRequestsByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task RemoveIssuesByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence;

public class WorkOrderRepository(AppDbContext dbContext) : IWorkOrderRepository
{
    public Task<List<WorkType>> GetWorkTypesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.WorkTypes.OrderBy(workType => workType.Code).ToListAsync(cancellationToken);
    }

    public Task<WorkType?> GetWorkTypeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.WorkTypes.FirstOrDefaultAsync(workType => workType.Id == id, cancellationToken);
    }

    public Task<WorkType?> GetWorkTypeByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return dbContext.WorkTypes.FirstOrDefaultAsync(workType => workType.Code == code, cancellationToken);
    }

    public async Task AddWorkTypeAsync(WorkType workType, CancellationToken cancellationToken = default)
    {
        await dbContext.WorkTypes.AddAsync(workType, cancellationToken);
    }

    public void RemoveWorkType(WorkType workType)
    {
        dbContext.WorkTypes.Remove(workType);
    }

    public Task<List<WorkOrder>> GetWorkOrdersAsync(CancellationToken cancellationToken = default)
    {
        return WorkOrderQuery().OrderByDescending(workOrder => workOrder.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<List<WorkOrder>> GetWorkOrdersForCrewAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        return WorkOrderQuery()
            .Where(workOrder => workOrder.CrewId == crewId)
            .OrderByDescending(workOrder => workOrder.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<WorkOrder>> GetWorkOrdersForSubcontractorAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        return WorkOrderQuery()
            .Where(workOrder => workOrder.SubcontractorId == subcontractorId)
            .OrderByDescending(workOrder => workOrder.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkOrder?> GetWorkOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return WorkOrderQuery()
            .Include(workOrder => workOrder.DailyReports)
            .ThenInclude(dailyReport => dailyReport.WorkEntries)
            .Include(workOrder => workOrder.DailyReports)
            .ThenInclude(dailyReport => dailyReport.MaterialUsages)
            .FirstOrDefaultAsync(workOrder => workOrder.Id == id, cancellationToken);
    }

    public Task<WorkOrder?> GetWorkOrderByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return dbContext.WorkOrders.FirstOrDefaultAsync(workOrder => workOrder.Number == number, cancellationToken);
    }

    public async Task<int> GetNextWorkOrderSequenceAsync(CancellationToken cancellationToken = default)
    {
        var maxNumber = await dbContext.WorkOrders
            .Where(wo => wo.Number.StartsWith("Z"))
            .Select(wo => wo.Number.Substring(1))
            .ToListAsync(cancellationToken);
        
        var maxSequence = maxNumber
            .Select(n => int.TryParse(n, out var num) ? num : 0)
            .DefaultIfEmpty(0)
            .Max();
        
        return maxSequence + 1;
    }

    public async Task AddWorkOrderAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        await dbContext.WorkOrders.AddAsync(workOrder, cancellationToken);
    }

    public async Task AddOrderedWorkAsync(OrderedWork orderedWork, CancellationToken cancellationToken = default)
    {
        await dbContext.OrderedWorks.AddAsync(orderedWork, cancellationToken);
    }

    public async Task AddOrderedMaterialAsync(OrderedMaterial orderedMaterial, CancellationToken cancellationToken = default)
    {
        await dbContext.OrderedMaterials.AddAsync(orderedMaterial, cancellationToken);
    }

    public void RemoveOrderedWork(OrderedWork orderedWork)
    {
        dbContext.OrderedWorks.Remove(orderedWork);
    }

    public void RemoveOrderedMaterial(OrderedMaterial orderedMaterial)
    {
        dbContext.OrderedMaterials.Remove(orderedMaterial);
    }

    public Task<OrderedWork?> GetOrderedWorkByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.OrderedWorks.Include(work => work.WorkType).FirstOrDefaultAsync(work => work.Id == id, cancellationToken);
    }

    public Task<OrderedMaterial?> GetOrderedMaterialByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.OrderedMaterials.Include(material => material.Material).FirstOrDefaultAsync(material => material.Id == id, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<WorkOrder> WorkOrderQuery()
    {
        return dbContext.WorkOrders
            .Include(workOrder => workOrder.Project)
            .Include(workOrder => workOrder.Section)
            .Include(workOrder => workOrder.Crew)
            .ThenInclude(crew => crew!.Worker)
            .Include(workOrder => workOrder.SubcontractorCrew)
            .ThenInclude(crew => crew!.CurrentForeman)
            .Include(workOrder => workOrder.Subcontractor)
            .Include(workOrder => workOrder.CreatedBy)
            .Include(workOrder => workOrder.OrderedWorks)
            .ThenInclude(orderedWork => orderedWork.WorkType)
            .Include(workOrder => workOrder.OrderedWorks)
            .ThenInclude(orderedWork => orderedWork.Section)
            .Include(workOrder => workOrder.OrderedWorks)
            .ThenInclude(orderedWork => orderedWork.Installation)
            .Include(workOrder => workOrder.OrderedMaterials)
            .ThenInclude(orderedMaterial => orderedMaterial.Material);
    }
}

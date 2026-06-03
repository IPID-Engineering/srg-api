using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.Warehouses;

public class MaterialService(
    IWarehouseRepository warehouse,
    IWorkOrderRepository workOrderRepository) : IMaterialService
{
    public async Task<MaterialResponse> CreateMaterialAsync(
        CreateMaterialRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Unit))
        {
            throw new ValidationException("Name and unit are required.");
        }

        var category = await warehouse.GetCategoryByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new ValidationException("Category not found.");

        var material = new Material
        {
            Name = request.Name.Trim(),
            CategoryId = category.Id,
            Unit = request.Unit.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        await warehouse.AddMaterialAsync(material, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);

        material.Category = category;
        return ToResponse(material);
    }

    public async Task<List<MaterialResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var materialUsages = await warehouse.GetMaterialsAsync(cancellationToken);
        return materialUsages.Select(ToResponse).ToList();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var material = await warehouse.GetMaterialByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Materiał nie został znaleziony.");

        var isInUse = await warehouse.IsMaterialInUseAsync(id, cancellationToken);
        if (isInUse)
        {
            throw new ValidationException("Nie można usunąć materiału, który jest używany w zleceniach, magazynie lub innych dokumentach.");
        }

        warehouse.RemoveMaterial(material);
        await warehouse.SaveChangesAsync(cancellationToken);
    }

    public async Task<MaterialAvailabilityResponse> CheckAvailabilityAsync(
        Guid materialId,
        CheckMaterialAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var material = await warehouse.GetMaterialByIdAsync(materialId, cancellationToken)
            ?? throw new KeyNotFoundException("Materiał nie został znaleziony.");

        var mainWarehouse = await warehouse.GetMainWarehouseAsync(cancellationToken)
            ?? throw new InvalidOperationException("Magazyn główny nie istnieje.");

        var stock = await warehouse.GetStockItemAsync(mainWarehouse.Id, materialId, cancellationToken);
        var currentStock = stock?.Quantity ?? 0;
        var reservedQuantity = stock?.ReservedQuantity ?? 0;
        var availableStock = stock?.AvailableQuantity ?? 0;

        var allWorkOrders = await workOrderRepository.GetWorkOrdersAsync(cancellationToken);
        var activeStatuses = new[] { WorkOrderStatus.Draft, WorkOrderStatus.Assigned, WorkOrderStatus.InProgress };
        var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(request.DaysAhead));

        var relevantWorkOrders = allWorkOrders
            .Where(wo => activeStatuses.Contains(wo.Status))
            .Where(wo => wo.Id != request.ExcludeWorkOrderId)
            .Where(wo => wo.OrderedMaterials.Any(om => om.MaterialId == materialId))
            .Where(wo => wo.PlannedEndDate == null || wo.PlannedEndDate <= cutoffDate)
            .ToList();

        var conflicts = new List<MaterialConflictInfo>();
        decimal totalPlannedInOtherOrders = 0;

        foreach (var workOrder in relevantWorkOrders)
        {
            var orderedMaterial = workOrder.OrderedMaterials.First(om => om.MaterialId == materialId);
            var plannedQuantity = orderedMaterial.PlannedQuantity;

            var issues = await warehouse.GetIssuesByWorkOrderAsync(workOrder.Id, cancellationToken);
            var issuedQuantity = issues
                .Where(i => i.Status == IssueStatus.Confirmed)
                .SelectMany(i => i.Items)
                .Where(item => item.MaterialId == materialId)
                .Sum(item => item.Quantity);

            var remainingNeeded = Math.Max(0, plannedQuantity - issuedQuantity);
            totalPlannedInOtherOrders += remainingNeeded;

            var crewName = workOrder.Crew?.Name ?? workOrder.SubcontractorCrew?.Name;

            conflicts.Add(new MaterialConflictInfo(
                workOrder.Id,
                workOrder.Number,
                workOrder.Project?.Name,
                crewName,
                workOrder.PlannedEndDate,
                plannedQuantity,
                issuedQuantity,
                remainingNeeded,
                0
            ));
        }

        var afterAllocationAvailable = availableStock - request.Quantity;
        var potentialShortage = totalPlannedInOtherOrders - afterAllocationAvailable;
        var hasConflict = potentialShortage > 0 && conflicts.Count != 0;

        if (hasConflict)
        {
            var remainingAfterAllocation = afterAllocationAvailable;
            var updatedConflicts = new List<MaterialConflictInfo>();

            foreach (var conflict in conflicts.OrderBy(c => c.PlannedEndDate ?? DateOnly.MaxValue))
            {
                var shortage = Math.Max(0, conflict.RemainingNeeded - remainingAfterAllocation);
                remainingAfterAllocation = Math.Max(0, remainingAfterAllocation - conflict.RemainingNeeded);

                updatedConflicts.Add(conflict with { ShortageIfProceeded = shortage });
            }

            conflicts = updatedConflicts.Where(c => c.ShortageIfProceeded > 0).ToList();
        }
        else
        {
            conflicts = [];
        }

        var severity = hasConflict switch
        {
            false => null,
            true when potentialShortage > availableStock * 0.5m => "high",
            true when potentialShortage > availableStock * 0.2m => "medium",
            _ => "low"
        };

        return new MaterialAvailabilityResponse(
            materialId,
            material.Name,
            material.Unit,
            currentStock,
            reservedQuantity,
            availableStock,
            request.Quantity,
            totalPlannedInOtherOrders,
            afterAllocationAvailable,
            hasConflict,
            severity,
            conflicts
        );
    }

    private static MaterialResponse ToResponse(Material material)
    {
        return new MaterialResponse(
            material.Id,
            material.Name,
            material.CategoryId,
            material.Category?.Name ?? string.Empty,
            material.Unit,
            material.CreatedAt);
    }
}

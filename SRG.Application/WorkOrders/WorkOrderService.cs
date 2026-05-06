using System.ComponentModel.DataAnnotations;
using SRG.Application.Audit;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.WorkOrders;

public class WorkOrderService(
    IWorkOrderRepository workOrderRepository,
    IConstructionRepository constructionRepository,
    IWarehouseRepository warehouseRepository,
    IAuditService auditService) : IWorkOrderService
{
    public async Task<List<WorkOrderResponse>> GetWorkOrdersAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var workOrders = role switch
        {
            nameof(UserRole.Foreman) => await GetForemanWorkOrdersAsync(userId, cancellationToken),
            _ => await workOrderRepository.GetWorkOrdersAsync(cancellationToken),
        };

        return workOrders.Select(ToResponse).ToList();
    }

    public async Task<WorkOrderResponse> GetWorkOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ToResponse(await GetWorkOrderEntityAsync(id, cancellationToken));
    }

    public async Task<WorkOrderResponse> CreateWorkOrderAsync(
        CreateWorkOrderRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default)
    {
        await ValidateWorkOrderReferencesAsync(request.ProjectId, request.SectionId, request.CrewId, request.SubcontractorCrewId, request.SubcontractorId, cancellationToken);

        var sequence = await workOrderRepository.GetNextWorkOrderSequenceAsync(cancellationToken);
        var number = $"Z{sequence:D4}";

        var workOrder = new WorkOrder
        {
            Number = number,
            ProjectId = request.ProjectId,
            SectionId = request.SectionId,
            CrewId = request.CrewId,
            SubcontractorCrewId = request.SubcontractorCrewId,
            SubcontractorId = request.SubcontractorId,
            CreatedById = createdById,
            Status = request.CrewId is null && request.SubcontractorCrewId is null && request.SubcontractorId is null ? WorkOrderStatus.Draft : WorkOrderStatus.Assigned,
            Description = OptionalText(request.Description),
            DocumentationUrl = OptionalText(request.DocumentationUrl),
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = request.PlannedEndDate,
            CreatedAt = DateTime.UtcNow,
        };

        await workOrderRepository.AddWorkOrderAsync(workOrder, cancellationToken);
        await workOrderRepository.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(createdById, "CREATE_WORK_ORDER", "WorkOrder", workOrder.Id, new
        {
            workOrder.Number,
            workOrder.ProjectId,
            workOrder.CrewId,
            workOrder.SubcontractorCrewId,
            workOrder.SubcontractorId,
            workOrder.Status,
        }, cancellationToken);

        return ToResponse(workOrder);
    }

    public async Task<WorkOrderResponse> UpdateWorkOrderAsync(Guid id, UpdateWorkOrderRequest request, CancellationToken cancellationToken = default)
    {
        var workOrder = await GetWorkOrderEntityAsync(id, cancellationToken);
        await ValidateWorkOrderReferencesAsync(workOrder.ProjectId, request.SectionId, request.CrewId, request.SubcontractorCrewId, request.SubcontractorId, cancellationToken);

        var wasAssigned = workOrder.CrewId != request.CrewId || workOrder.SubcontractorCrewId != request.SubcontractorCrewId || workOrder.SubcontractorId != request.SubcontractorId;
        workOrder.SectionId = request.SectionId;
        workOrder.CrewId = request.CrewId;
        workOrder.SubcontractorCrewId = request.SubcontractorCrewId;
        workOrder.SubcontractorId = request.SubcontractorId;
        workOrder.Status = request.Status;
        workOrder.Description = OptionalText(request.Description);
        workOrder.DocumentationUrl = OptionalText(request.DocumentationUrl);
        workOrder.PlannedStartDate = request.PlannedStartDate;
        workOrder.PlannedEndDate = request.PlannedEndDate;
        await workOrderRepository.SaveChangesAsync(cancellationToken);

        if (wasAssigned)
        {
            await auditService.LogActionAsync(workOrder.CreatedById, "ASSIGN_WORK_ORDER", "WorkOrder", workOrder.Id, new
            {
                workOrder.CrewId,
                workOrder.SubcontractorId,
            }, cancellationToken);
        }

        return ToResponse(workOrder);
    }

    public async Task<WorkOrderResponse> AddOrderedWorkAsync(
        Guid id,
        AddOrderedWorkRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var workOrder = await GetWorkOrderEntityAsync(id, cancellationToken);
        var workType = await workOrderRepository.GetWorkTypeByIdAsync(request.WorkTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("WorkType was not found.");
        if (!workType.IsActive)
        {
            throw new ValidationException("Only active WorkTypes can be added to WorkOrders.");
        }

        var orderedWork = new OrderedWork
        {
            WorkOrderId = workOrder.Id,
            WorkTypeId = workType.Id,
            SectionId = request.SectionId,
            InstallationId = request.InstallationId,
            Description = OptionalText(request.Description),
            PlannedQuantity = PositiveQuantity(request.PlannedQuantity),
            Unit = RequiredText(request.Unit, "Unit"),
            CreatedAt = DateTime.UtcNow,
        };

        await workOrderRepository.AddOrderedWorkAsync(orderedWork, cancellationToken);
        await workOrderRepository.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(userId, "ADD_ORDERED_WORK", "OrderedWork", orderedWork.Id, new
        {
            orderedWork.WorkOrderId,
            orderedWork.WorkTypeId,
            orderedWork.PlannedQuantity,
            orderedWork.Unit,
        }, cancellationToken);

        return ToResponse(await GetWorkOrderEntityAsync(id, cancellationToken));
    }

    public async Task<WorkOrderResponse> AddOrderedMaterialAsync(
        Guid id,
        AddOrderedMaterialRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var workOrder = await GetWorkOrderEntityAsync(id, cancellationToken);
        _ = await warehouseRepository.GetMaterialByIdAsync(request.MaterialId, cancellationToken)
            ?? throw new KeyNotFoundException("Material was not found.");

        var orderedMaterial = new OrderedMaterial
        {
            WorkOrderId = workOrder.Id,
            MaterialId = request.MaterialId,
            PlannedQuantity = PositiveQuantity(request.PlannedQuantity),
            Unit = RequiredText(request.Unit, "Unit"),
            CreatedAt = DateTime.UtcNow,
        };

        await workOrderRepository.AddOrderedMaterialAsync(orderedMaterial, cancellationToken);
        await workOrderRepository.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(userId, "ADD_ORDERED_MATERIAL", "OrderedMaterial", orderedMaterial.Id, new
        {
            orderedMaterial.WorkOrderId,
            orderedMaterial.MaterialId,
            orderedMaterial.PlannedQuantity,
            orderedMaterial.Unit,
        }, cancellationToken);

        return ToResponse(await GetWorkOrderEntityAsync(id, cancellationToken));
    }

    public async Task<WorkOrderProgressResponse> GetProgressAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workOrder = await GetWorkOrderEntityAsync(id, cancellationToken);
        var plannedWork = workOrder.OrderedWorks.Sum(work => work.PlannedQuantity);
        var reportedWork = workOrder.DailyReports.SelectMany(report => report.WorkEntries).Sum(entry => entry.Quantity);
        var plannedMaterials = workOrder.OrderedMaterials.Sum(material => material.PlannedQuantity);
        var usedMaterials = workOrder.DailyReports.SelectMany(report => report.MaterialUsages).Sum(usage => usage.Quantity);
        var plannedTotal = plannedWork + plannedMaterials;
        var actualTotal = reportedWork + usedMaterials;
        var progress = plannedTotal == 0 ? 0 : Math.Round(Math.Min(actualTotal / plannedTotal * 100, 100), 2);

        return new WorkOrderProgressResponse(id, plannedWork, reportedWork, plannedMaterials, usedMaterials, progress);
    }

    public async Task RemoveOrderedWorkAsync(Guid workOrderId, Guid orderedWorkId, Guid userId, CancellationToken cancellationToken = default)
    {
        var workOrder = await GetWorkOrderEntityAsync(workOrderId, cancellationToken);
        var orderedWork = workOrder.OrderedWorks.FirstOrDefault(w => w.Id == orderedWorkId)
            ?? throw new KeyNotFoundException("OrderedWork was not found.");

        workOrderRepository.RemoveOrderedWork(orderedWork);
        await workOrderRepository.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(userId, "REMOVE_ORDERED_WORK", "OrderedWork", orderedWorkId, new
        {
            WorkOrderId = workOrderId,
            orderedWork.WorkTypeId,
            orderedWork.PlannedQuantity,
        }, cancellationToken);
    }

    public async Task RemoveOrderedMaterialAsync(Guid workOrderId, Guid orderedMaterialId, Guid userId, CancellationToken cancellationToken = default)
    {
        var workOrder = await GetWorkOrderEntityAsync(workOrderId, cancellationToken);
        var orderedMaterial = workOrder.OrderedMaterials.FirstOrDefault(m => m.Id == orderedMaterialId)
            ?? throw new KeyNotFoundException("OrderedMaterial was not found.");

        var issues = await warehouseRepository.GetIssuesByWorkOrderAsync(workOrderId, cancellationToken);
        var issuedQuantity = issues
            .Where(i => i.Status == IssueStatus.Confirmed)
            .SelectMany(i => i.Items)
            .Where(item => item.MaterialId == orderedMaterial.MaterialId)
            .Sum(item => item.Quantity);

        if (issuedQuantity > 0)
        {
            throw new ValidationException("Nie można usunąć materiału, który został już wydany.");
        }

        workOrderRepository.RemoveOrderedMaterial(orderedMaterial);
        await workOrderRepository.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(userId, "REMOVE_ORDERED_MATERIAL", "OrderedMaterial", orderedMaterialId, new
        {
            WorkOrderId = workOrderId,
            orderedMaterial.MaterialId,
            orderedMaterial.PlannedQuantity,
        }, cancellationToken);
    }

    public async Task<WorkOrderResponse> AcceptWorkOrderAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var workOrder = await GetWorkOrderEntityAsync(id, cancellationToken);

        if (workOrder.Status != WorkOrderStatus.Assigned)
        {
            throw new ValidationException("Można zaakceptować tylko zlecenie o statusie 'Przypisane'.");
        }

        workOrder.Status = WorkOrderStatus.InProgress;
        await workOrderRepository.SaveChangesAsync(cancellationToken);

        await auditService.LogActionAsync(userId, "ACCEPT_WORK_ORDER", "WorkOrder", id, new
        {
            workOrder.Number,
            PreviousStatus = WorkOrderStatus.Assigned.ToString(),
            NewStatus = WorkOrderStatus.InProgress.ToString(),
        }, cancellationToken);

        return ToResponse(workOrder);
    }

    private async Task<List<WorkOrder>> GetForemanWorkOrdersAsync(Guid userId, CancellationToken cancellationToken)
    {
        var workOrders = await workOrderRepository.GetWorkOrdersAsync(cancellationToken);
        var ownedCrewIds = workOrders
            .Where(workOrder => workOrder.Crew is not null)
            .Select(workOrder => workOrder.Crew!)
            .Where(crew => crew.CreatedById == userId || crew.Worker.Any(worker => worker.CreatedById == userId))
            .Select(crew => crew.Id)
            .ToHashSet();

        return workOrders.Where(workOrder => workOrder.CrewId is not null && ownedCrewIds.Contains(workOrder.CrewId.Value)).ToList();
    }

    private async Task<WorkOrder> GetWorkOrderEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        return await workOrderRepository.GetWorkOrderByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("WorkOrder was not found.");
    }

    private async Task ValidateWorkOrderReferencesAsync(
        Guid projectId,
        Guid? sectionId,
        Guid? crewId,
        Guid? subcontractorCrewId,
        Guid? subcontractorId,
        CancellationToken cancellationToken)
    {
        _ = await constructionRepository.GetProjectByIdAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        if (sectionId is not null)
        {
            var sections = await constructionRepository.GetSectionsByProjectAsync(projectId, cancellationToken);
            if (sections.All(section => section.Id != sectionId.Value))
            {
                throw new ValidationException("Section must belong to WorkOrder Project.");
            }
        }

        if (crewId is not null)
        {
            var crew = await constructionRepository.GetCrewByIdAsync(crewId.Value, cancellationToken);
            if (crew is null)
            {
                throw new KeyNotFoundException("Crew was not found.");
            }
            if (crew.ProjectId != projectId)
            {
                throw new ValidationException("Crew must belong to WorkOrder Project.");
            }
        }

        if (subcontractorCrewId is not null)
        {
            _ = await constructionRepository.GetSubcontractorCrewByIdAsync(subcontractorCrewId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("SubcontractorCrew was not found.");
        }

        if (subcontractorId is not null)
        {
            var assignment = await constructionRepository.GetProjectSubcontractorAsync(projectId, subcontractorId.Value, cancellationToken);
            if (assignment is null)
            {
                throw new ValidationException("Subcontractor must be assigned to WorkOrder Project.");
            }
        }
    }

    private static WorkOrderResponse ToResponse(WorkOrder workOrder)
    {
        var foremanName = workOrder.SubcontractorCrew?.CurrentForeman is { } foreman
            ? $"{foreman.FirstName} {foreman.LastName}"
            : null;

        var createdByName = workOrder.CreatedBy is { } creator
            ? $"{creator.FirstName} {creator.LastName}"
            : null;

        return new WorkOrderResponse(
            workOrder.Id,
            workOrder.Number,
            workOrder.ProjectId,
            workOrder.Project?.Name,
            workOrder.SectionId,
            workOrder.Section?.Name,
            workOrder.CrewId,
            workOrder.Crew?.Name,
            workOrder.SubcontractorCrewId,
            workOrder.SubcontractorCrew?.Name,
            foremanName,
            workOrder.SubcontractorId,
            workOrder.Subcontractor?.FullName,
            workOrder.CreatedById,
            createdByName,
            workOrder.Status,
            workOrder.Description,
            workOrder.DocumentationUrl,
            workOrder.PlannedStartDate,
            workOrder.PlannedEndDate,
            workOrder.CreatedAt,
            workOrder.OrderedWorks.Select(work => new OrderedWorkResponse(
                work.Id,
                work.WorkOrderId,
                work.WorkTypeId,
                work.WorkType?.Code,
                work.WorkType?.Name,
                work.SectionId,
                work.Section?.Name,
                work.InstallationId,
                work.Installation?.Name,
                work.Description,
                work.PlannedQuantity,
                work.Unit,
                work.CreatedAt)).ToList(),
            workOrder.OrderedMaterials.Select(material => new OrderedMaterialResponse(
                material.Id,
                material.WorkOrderId,
                material.MaterialId,
                material.Material?.Name,
                material.PlannedQuantity,
                material.Unit,
                material.CreatedAt)).ToList());
    }

    private static decimal PositiveQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }

        return quantity;
    }

    private static string RequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? OptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

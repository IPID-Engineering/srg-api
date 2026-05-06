using System.ComponentModel.DataAnnotations;
using SRG.Application.Audit;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.Warehouses;

public class IssueService(
    IWarehouseRepository warehouse,
    IWorkOrderRepository workOrderRepository,
    IAuditService auditService) : IIssueService
{
    public async Task<List<IssueResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var issues = await warehouse.GetIssuesAsync(cancellationToken);
        return issues.Select(ToResponse).ToList();
    }

    public async Task<List<IssueResponse>> GetByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var issues = await warehouse.GetIssuesByWorkOrderAsync(workOrderId, cancellationToken);
        return issues.Select(ToResponse).ToList();
    }

    public async Task<IssueResponse> CreateIssueAsync(
        CreateIssueRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default)
    {
        var workOrder = await workOrderRepository.GetWorkOrderByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new KeyNotFoundException("Work order was not found.");

        var crewOwnerId = workOrder.CrewId ?? workOrder.SubcontractorCrewId;
        if (!crewOwnerId.HasValue)
        {
            throw new ValidationException("Work order must have an assigned crew to create an issue.");
        }

        var main = await warehouse.GetMainWarehouseAsync(cancellationToken)
            ?? throw new InvalidOperationException("Main warehouse is missing.");

        var destination = await warehouse.GetSubWarehouseByOwnerAsync(crewOwnerId.Value, cancellationToken);
        if (destination == null)
        {
            var crewName = workOrder.Crew?.Name ?? workOrder.SubcontractorCrew?.Name ?? "Brygada";
            destination = new Warehouse
            {
                Name = crewName,
                Type = WarehouseType.Sub,
                OwnerId = crewOwnerId.Value,
            };
            await warehouse.AddWarehouseAsync(destination, cancellationToken);
            await warehouse.SaveChangesAsync(cancellationToken);
        }

        var issue = new Issue
        {
            WorkOrderId = workOrder.Id,
            FromWarehouseId = main.Id,
            ToWarehouseId = destination.Id,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow,
            Status = IssueStatus.Draft,
        };

        await warehouse.AddIssueAsync(issue, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);

        issue.WorkOrder = workOrder;

        await auditService.LogActionAsync(createdById, "CREATE_ISSUE", "Issue", issue.Id, new
        {
            issue.WorkOrderId,
            issue.FromWarehouseId,
            issue.ToWarehouseId,
            issue.Status,
        }, cancellationToken);

        return ToResponse(issue);
    }

    public async Task<IssueResponse> AddItemAsync(
        Guid issueId,
        AddIssueItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var issue = await GetDraftIssueAsync(issueId, cancellationToken);
        _ = await warehouse.GetMaterialByIdAsync(request.MaterialId, cancellationToken)
            ?? throw new KeyNotFoundException("Material was not found.");

        if (request.Quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }

        await warehouse.AddIssueItemAsync(new IssueItem
        {
            IssueId = issue.Id,
            MaterialId = request.MaterialId,
            Quantity = request.Quantity,
        }, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);

        return ToResponse(await GetIssueAsync(issueId, cancellationToken));
    }

    public async Task<IssueResponse> ConfirmIssueAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var confirmed = await GetIssueAsync(issueId, cancellationToken);

        await warehouse.ExecuteInTransactionAsync(async () =>
        {
            var issue = await GetDraftIssueAsync(issueId, cancellationToken);

            if (issue.Items.Count == 0)
            {
                throw new ValidationException("Cannot confirm Issue without items.");
            }

            foreach (var item in issue.Items)
            {
                await StockService.DecreaseStockAsync(warehouse, issue.FromWarehouseId, item.MaterialId, item.Quantity, StockMovementSourceType.Issue, issue.Id, issue.CreatedById, cancellationToken);
                await StockService.IncreaseStockAsync(warehouse, issue.ToWarehouseId, item.MaterialId, item.Quantity, StockMovementSourceType.Issue, issue.Id, issue.CreatedById, cancellationToken);
            }

            issue.Status = IssueStatus.Confirmed;
            await warehouse.SaveChangesAsync(cancellationToken);
            confirmed = issue;
        }, cancellationToken);
        await auditService.LogActionAsync(confirmed.CreatedById, "CONFIRM_ISSUE", "Issue", confirmed.Id, new
        {
            confirmed.FromWarehouseId,
            confirmed.ToWarehouseId,
            Items = confirmed.Items.Select(item => new { item.MaterialId, item.Quantity }),
        }, cancellationToken);

        return ToResponse(confirmed);
    }

    private async Task<Issue> GetDraftIssueAsync(Guid id, CancellationToken cancellationToken)
    {
        var issue = await GetIssueAsync(id, cancellationToken);

        if (issue.Status != IssueStatus.Draft)
        {
            throw new ValidationException("Only Draft Issue can be changed.");
        }

        return issue;
    }

    private async Task<Issue> GetIssueAsync(Guid id, CancellationToken cancellationToken)
    {
        return await warehouse.GetIssueByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Issue was not found.");
    }

    private static IssueResponse ToResponse(Issue issue)
    {
        return new IssueResponse(
            issue.Id,
            issue.WorkOrderId,
            issue.WorkOrder?.Number ?? string.Empty,
            issue.FromWarehouseId,
            issue.ToWarehouseId,
            issue.ToWarehouse?.Name,
            issue.CreatedById,
            issue.CreatedAt,
            issue.Status,
            issue.Items.Select(item => new IssueItemResponse(
                item.Id,
                item.IssueId,
                item.MaterialId,
                item.Material?.Name,
                item.Material?.Unit,
                item.Quantity)).ToList());
    }
}

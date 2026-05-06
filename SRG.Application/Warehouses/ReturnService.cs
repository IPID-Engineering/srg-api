using System.ComponentModel.DataAnnotations;
using SRG.Application.Audit;
using SRG.Application.Common;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.Warehouses;

public class ReturnService(
    IWarehouseRepository warehouse,
    IAuditService auditService,
    ICurrentUserContext currentUserContext) : IReturnService
{
    public async Task<ReturnResponse> CreateReturnAsync(Guid foremanId, CancellationToken cancellationToken = default)
    {
        var source = await WarehouseService.EnsureSubWarehouseAsync(warehouse, foremanId, cancellationToken);
        var destination = await warehouse.GetMainWarehouseAsync(cancellationToken)
            ?? throw new InvalidOperationException("Main warehouse is missing.");

        var returnDoc = new Return
        {
            FromWarehouseId = source.Id,
            ToWarehouseId = destination.Id,
            CreatedById = foremanId,
            Status = ReturnStatus.Draft,
            CreatedAt = DateTime.UtcNow,
        };

        await warehouse.AddReturnAsync(returnDoc, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(foremanId, "CREATE_RETURN", "Return", returnDoc.Id, new
        {
            returnDoc.FromWarehouseId,
            returnDoc.ToWarehouseId,
            returnDoc.Status,
        }, cancellationToken);

        return ToResponse(returnDoc);
    }

    public async Task<ReturnResponse> AddItemAsync(Guid returnId, AddReturnItemRequest request, CancellationToken cancellationToken = default)
    {
        var returnDoc = await GetDraftReturnAsync(returnId, cancellationToken);
        _ = await warehouse.GetMaterialByIdAsync(request.MaterialId, cancellationToken)
            ?? throw new KeyNotFoundException("Material was not found.");

        if (request.Quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }

        await warehouse.AddReturnItemAsync(new ReturnItem
        {
            ReturnId = returnDoc.Id,
            MaterialId = request.MaterialId,
            Quantity = request.Quantity,
        }, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);

        return ToResponse(await GetReturnAsync(returnId, cancellationToken));
    }

    public async Task<ReturnResponse> SubmitAsync(Guid returnId, CancellationToken cancellationToken = default)
    {
        var returnDoc = await GetDraftReturnAsync(returnId, cancellationToken);

        if (returnDoc.Items.Count == 0)
        {
            throw new ValidationException("Cannot submit Return without items.");
        }

        returnDoc.Status = ReturnStatus.Submitted;
        await warehouse.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(returnDoc.CreatedById, "SUBMIT_RETURN", "Return", returnDoc.Id, new
        {
            returnDoc.Status,
            Items = returnDoc.Items.Select(item => new { item.MaterialId, item.Quantity }),
        }, cancellationToken);

        return ToResponse(returnDoc);
    }

    public async Task<ReturnResponse> ApproveAsync(Guid returnId, CancellationToken cancellationToken = default)
    {
        var approved = await GetReturnAsync(returnId, cancellationToken);

        await warehouse.ExecuteInTransactionAsync(async () =>
        {
            var returnDoc = await GetReturnAsync(returnId, cancellationToken);

            if (returnDoc.Status != ReturnStatus.Submitted)
            {
                throw new ValidationException("Only Submitted Return can be approved.");
            }

            foreach (var item in returnDoc.Items)
            {
                await StockService.DecreaseStockAsync(warehouse, returnDoc.FromWarehouseId, item.MaterialId, item.Quantity, StockMovementSourceType.Return, returnDoc.Id, currentUserContext.UserId ?? returnDoc.CreatedById, cancellationToken);
                await StockService.IncreaseStockAsync(warehouse, returnDoc.ToWarehouseId, item.MaterialId, item.Quantity, StockMovementSourceType.Return, returnDoc.Id, currentUserContext.UserId ?? returnDoc.CreatedById, cancellationToken);
            }

            returnDoc.Status = ReturnStatus.Approved;
            await warehouse.SaveChangesAsync(cancellationToken);
            approved = returnDoc;
        }, cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? Guid.Empty, "APPROVE_RETURN", "Return", approved.Id, new
        {
            approved.FromWarehouseId,
            approved.ToWarehouseId,
            Items = approved.Items.Select(item => new { item.MaterialId, item.Quantity }),
        }, cancellationToken);

        return ToResponse(approved);
    }

    public async Task<List<ReturnResponse>> GetSubmittedAsync(CancellationToken cancellationToken = default)
    {
        var returns = await warehouse.GetReturnsByStatusAsync(ReturnStatus.Submitted, cancellationToken);
        return returns.Select(ToResponse).ToList();
    }

    private async Task<Return> GetDraftReturnAsync(Guid id, CancellationToken cancellationToken)
    {
        var returnDoc = await GetReturnAsync(id, cancellationToken);

        if (returnDoc.Status != ReturnStatus.Draft)
        {
            throw new ValidationException("Only Draft Return can be changed.");
        }

        return returnDoc;
    }

    private async Task<Return> GetReturnAsync(Guid id, CancellationToken cancellationToken)
    {
        return await warehouse.GetReturnByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Return was not found.");
    }

    private static ReturnResponse ToResponse(Return returnDoc)
    {
        return new ReturnResponse(
            returnDoc.Id,
            returnDoc.FromWarehouseId,
            returnDoc.ToWarehouseId,
            returnDoc.CreatedById,
            returnDoc.CreatedAt,
            returnDoc.Status,
            returnDoc.Items.Select(item => new ReturnItemResponse(
                item.Id,
                item.ReturnId,
                item.MaterialId,
                item.Material?.Name,
                item.Quantity)).ToList());
    }
}

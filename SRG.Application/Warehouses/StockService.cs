using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.Warehouses;

public static class StockService
{
    public static async Task DecreaseStockAsync(
        IWarehouseRepository warehouse,
        Guid warehouseId,
        Guid materialId,
        decimal quantity,
        StockMovementSourceType sourceType,
        Guid sourceId,
        Guid createdById,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }

        var stock = await warehouse.GetStockItemAsync(warehouseId, materialId, cancellationToken)
            ?? throw new ValidationException("Material is not available in source warehouse.");

        if (stock.Quantity < quantity)
        {
            throw new ValidationException("Cannot move more stock than available.");
        }

        stock.Quantity -= quantity;
        await AddMovementAsync(warehouse, warehouseId, materialId, quantity, StockMovementDirection.Out, sourceType, sourceId, createdById, cancellationToken);
    }

    public static async Task IncreaseStockAsync(
        IWarehouseRepository warehouse,
        Guid warehouseId,
        Guid materialId,
        decimal quantity,
        StockMovementSourceType sourceType,
        Guid sourceId,
        Guid createdById,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }

        var stock = await warehouse.GetStockItemAsync(warehouseId, materialId, cancellationToken);

        if (stock is null)
        {
            await warehouse.AddStockAsync(new WarehouseStock
            {
                WarehouseId = warehouseId,
                MaterialId = materialId,
                Quantity = quantity,
            }, cancellationToken);
            await AddMovementAsync(warehouse, warehouseId, materialId, quantity, StockMovementDirection.In, sourceType, sourceId, createdById, cancellationToken);
            return;
        }

        stock.Quantity += quantity;
        await AddMovementAsync(warehouse, warehouseId, materialId, quantity, StockMovementDirection.In, sourceType, sourceId, createdById, cancellationToken);
    }

    private static async Task AddMovementAsync(
        IWarehouseRepository warehouse,
        Guid warehouseId,
        Guid materialId,
        decimal quantity,
        StockMovementDirection direction,
        StockMovementSourceType sourceType,
        Guid sourceId,
        Guid createdById,
        CancellationToken cancellationToken)
    {
        await warehouse.AddMovementAsync(new StockMovement
        {
            WarehouseId = warehouseId,
            MaterialId = materialId,
            Quantity = quantity,
            Direction = direction,
            SourceType = sourceType,
            SourceId = sourceId,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);
    }
}

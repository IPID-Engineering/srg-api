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

        var quantityBefore = stock.Quantity;
        stock.Quantity -= quantity;
        var quantityAfter = stock.Quantity;
        
        await AddMovementAsync(warehouse, warehouseId, materialId, quantity, quantityBefore, quantityAfter, StockMovementDirection.Out, sourceType, sourceId, createdById, cancellationToken);
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
            await AddMovementAsync(warehouse, warehouseId, materialId, quantity, 0, quantity, StockMovementDirection.In, sourceType, sourceId, createdById, cancellationToken);
            return;
        }

        var quantityBefore = stock.Quantity;
        stock.Quantity += quantity;
        var quantityAfter = stock.Quantity;
        
        await AddMovementAsync(warehouse, warehouseId, materialId, quantity, quantityBefore, quantityAfter, StockMovementDirection.In, sourceType, sourceId, createdById, cancellationToken);
    }

    public static async Task ReserveMaterialAsync(
        IWarehouseRepository warehouse,
        Guid warehouseId,
        Guid materialId,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0) return;

        var stock = await warehouse.GetStockItemAsync(warehouseId, materialId, cancellationToken)
            ?? throw new ValidationException("Material is not available in warehouse.");

        if (stock.AvailableQuantity < quantity)
        {
            throw new ValidationException($"Niewystarczająca ilość dostępna. Dostępne: {stock.AvailableQuantity}, potrzebne: {quantity}");
        }

        stock.ReservedQuantity += quantity;
    }

    public static async Task ReleaseReservationAsync(
        IWarehouseRepository warehouse,
        Guid warehouseId,
        Guid materialId,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0) return;

        var stock = await warehouse.GetStockItemAsync(warehouseId, materialId, cancellationToken);
        if (stock is null) return;

        stock.ReservedQuantity = Math.Max(0, stock.ReservedQuantity - quantity);
    }

    public static async Task ConsumeReservedMaterialAsync(
        IWarehouseRepository warehouse,
        Guid warehouseId,
        Guid materialId,
        decimal quantity,
        StockMovementSourceType sourceType,
        Guid sourceId,
        Guid createdById,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0) return;

        var stock = await warehouse.GetStockItemAsync(warehouseId, materialId, cancellationToken);
        if (stock is null) return;

        var quantityBefore = stock.Quantity;
        stock.ReservedQuantity = Math.Max(0, stock.ReservedQuantity - quantity);
        stock.Quantity = Math.Max(0, stock.Quantity - quantity);
        var quantityAfter = stock.Quantity;
        
        await AddMovementAsync(warehouse, warehouseId, materialId, quantity, quantityBefore, quantityAfter, StockMovementDirection.Out, sourceType, sourceId, createdById, cancellationToken);
    }

    private static async Task AddMovementAsync(
        IWarehouseRepository warehouse,
        Guid warehouseId,
        Guid materialId,
        decimal quantity,
        decimal quantityBefore,
        decimal quantityAfter,
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
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            Direction = direction,
            SourceType = sourceType,
            SourceId = sourceId,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);
    }
}

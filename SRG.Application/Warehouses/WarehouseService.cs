using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.Warehouses;

public class WarehouseService(IWarehouseRepository warehouse, IConstructionRepository construction) : IWarehouseService
{
    public async Task<WarehouseResponse> GetMainWarehouseAsync(CancellationToken cancellationToken = default)
    {
        var main = await warehouse.GetMainWarehouseAsync(cancellationToken)
            ?? throw new InvalidOperationException("Main warehouse is missing.");

        return ToResponse(main);
    }

    public async Task<WarehouseResponse> GetSubWarehouseAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var subWarehouse = await EnsureSubWarehouseAsync(warehouse, ownerId, cancellationToken);
        return ToResponse(subWarehouse);
    }

    public async Task<WarehouseResponse> GetForemanWarehouseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var crews = await construction.GetAllCrewsAsync(cancellationToken);
        var userCrew = crews.FirstOrDefault(c => c.CreatedById == userId);
        
        if (userCrew is not null)
        {
            var subWarehouse = await EnsureSubWarehouseAsync(warehouse, userCrew.Id, cancellationToken);
            return ToResponse(subWarehouse);
        }

        throw new KeyNotFoundException("Nie znaleziono brygady dla tego brygadzisty.");
    }

    public async Task<List<WarehouseResponse>> GetAllSubWarehousesAsync(CancellationToken cancellationToken = default)
    {
        var crews = await construction.GetAllCrewsAsync(cancellationToken);
        var subcontractorCrews = await construction.GetAllSubcontractorCrewsAsync(cancellationToken);
        var existingWarehouses = await warehouse.GetAllSubWarehousesAsync(cancellationToken);
        var warehousesByOwner = existingWarehouses.ToDictionary(w => w.OwnerId ?? Guid.Empty);
        
        var result = new List<Warehouse>();
        
        foreach (var crew in crews)
        {
            if (warehousesByOwner.TryGetValue(crew.Id, out var existing))
            {
                result.Add(existing);
            }
            else
            {
                var newWarehouse = new Warehouse
                {
                    Name = crew.Name,
                    Type = WarehouseType.Sub,
                    OwnerId = crew.Id,
                };
                await warehouse.AddWarehouseAsync(newWarehouse, cancellationToken);
                await warehouse.SaveChangesAsync(cancellationToken);
                result.Add(newWarehouse);
            }
        }

        foreach (var subCrew in subcontractorCrews)
        {
            if (warehousesByOwner.TryGetValue(subCrew.Id, out var existing))
            {
                result.Add(existing);
            }
            else
            {
                var newWarehouse = new Warehouse
                {
                    Name = subCrew.Name,
                    Type = WarehouseType.Sub,
                    OwnerId = subCrew.Id,
                };
                await warehouse.AddWarehouseAsync(newWarehouse, cancellationToken);
                await warehouse.SaveChangesAsync(cancellationToken);
                result.Add(newWarehouse);
            }
        }
        
        return result.OrderBy(w => w.Name).Select(ToResponse).ToList();
    }

    public async Task<List<StockResponse>> GetStockAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        _ = await warehouse.GetWarehouseByIdAsync(warehouseId, cancellationToken)
            ?? throw new KeyNotFoundException("Warehouse was not found.");

        var stock = await warehouse.GetStockAsync(warehouseId, cancellationToken);
        var categories = await warehouse.GetCategoriesAsync(cancellationToken);
        var categoryDict = categories.ToDictionary(c => c.Id);
        
        return stock.Select(s => ToResponseWithCategories(s, categoryDict)).ToList();
    }
    
    private static StockResponse ToResponseWithCategories(WarehouseStock stock, Dictionary<Guid, Category> categoryDict)
    {
        var material = stock.Material;
        Category? category = null;
        Category? parentCategory = null;
        
        if (material != null && categoryDict.TryGetValue(material.CategoryId, out var cat))
        {
            category = cat;
            if (cat.ParentCategoryId.HasValue && categoryDict.TryGetValue(cat.ParentCategoryId.Value, out var parent))
            {
                parentCategory = parent;
            }
        }
        
        return new StockResponse(
            stock.Id,
            stock.WarehouseId,
            stock.MaterialId,
            material?.Name ?? string.Empty,
            material?.Unit ?? string.Empty,
            stock.Quantity,
            category?.Id,
            category?.Name,
            parentCategory?.Id,
            parentCategory?.Name);
    }

    public async Task<List<StockMovementResponse>> GetMovementsAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        _ = await warehouse.GetWarehouseByIdAsync(warehouseId, cancellationToken)
            ?? throw new KeyNotFoundException("Warehouse was not found.");

        var movements = await warehouse.GetMovementsAsync(warehouseId, cancellationToken);
        return movements.Select(ToResponse).ToList();
    }

    public static async Task<Warehouse> EnsureSubWarehouseAsync(
        IWarehouseRepository warehouse,
        Guid foremanId,
        CancellationToken cancellationToken = default)
    {
        var existing = await warehouse.GetSubWarehouseByOwnerAsync(foremanId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var created = new Warehouse
        {
            Name = $"SubWarehouse {foremanId}",
            Type = WarehouseType.Sub,
            OwnerId = foremanId,
        };

        await warehouse.AddWarehouseAsync(created, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static WarehouseResponse ToResponse(Warehouse warehouse)
    {
        return new WarehouseResponse(warehouse.Id, warehouse.Name, warehouse.Type, warehouse.OwnerId);
    }

    private static StockResponse ToResponse(WarehouseStock stock)
    {
        var category = stock.Material?.Category;
        var parentCategory = category?.ParentCategory;
        
        return new StockResponse(
            stock.Id,
            stock.WarehouseId,
            stock.MaterialId,
            stock.Material?.Name ?? string.Empty,
            stock.Material?.Unit ?? string.Empty,
            stock.Quantity,
            category?.Id,
            category?.Name,
            parentCategory?.Id,
            parentCategory?.Name);
    }

    private static StockMovementResponse ToResponse(StockMovement movement)
    {
        return new StockMovementResponse(
            movement.Id,
            movement.WarehouseId,
            movement.MaterialId,
            movement.Material?.Name,
            movement.Quantity,
            movement.Direction,
            movement.SourceType,
            movement.SourceId,
            movement.CreatedById,
            movement.CreatedAt);
    }
}

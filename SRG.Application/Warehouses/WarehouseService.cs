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
            stock.ReservedQuantity,
            stock.AvailableQuantity,
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
        var categories = await warehouse.GetCategoriesAsync(cancellationToken);
        var categoryDict = categories.ToDictionary(c => c.Id);
        
        var result = new List<StockMovementResponse>();
        foreach (var m in movements)
        {
            string? sourceNumber = null;
            string? workOrderNumber = null;
            string? targetWarehouseName = null;
            string? categoryName = null;
            
            if (m.Material?.CategoryId != null && categoryDict.TryGetValue(m.Material.CategoryId, out var cat))
            {
                categoryName = cat.Name;
            }
            
            if (m.SourceType == StockMovementSourceType.Issue)
            {
                var issue = await warehouse.GetIssueBySourceIdAsync(m.SourceId, cancellationToken);
                if (issue != null)
                {
                    sourceNumber = issue.Number;
                    workOrderNumber = issue.WorkOrder?.Number;
                    targetWarehouseName = issue.ToWarehouse?.Name;
                }
            }
            else if (m.SourceType == StockMovementSourceType.GRV)
            {
                var grv = await warehouse.GetGoodsReceivedVoucherByIdAsync(m.SourceId, cancellationToken);
                sourceNumber = grv?.Number;
            }
            
            result.Add(new StockMovementResponse(
                m.Id,
                m.WarehouseId,
                m.MaterialId,
                m.Material?.Name,
                categoryName,
                m.Quantity,
                m.QuantityBefore,
                m.QuantityAfter,
                m.Direction,
                m.SourceType,
                m.SourceId,
                sourceNumber,
                workOrderNumber,
                targetWarehouseName,
                m.CreatedById,
                m.CreatedBy?.FirstName != null ? $"{m.CreatedBy.FirstName} {m.CreatedBy.LastName}" : null,
                m.CreatedAt));
        }
        
        return result;
    }

    public static async Task<Warehouse> EnsureSubWarehouseAsync(
        IWarehouseRepository warehouse,
        Guid ownerId,
        CancellationToken cancellationToken = default,
        string? crewName = null)
    {
        var existing = await warehouse.GetSubWarehouseByOwnerAsync(ownerId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var created = new Warehouse
        {
            Name = crewName ?? "Magazyn brygady",
            Type = WarehouseType.Sub,
            OwnerId = ownerId,
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
            stock.ReservedQuantity,
            stock.AvailableQuantity,
            category?.Id,
            category?.Name,
            parentCategory?.Id,
            parentCategory?.Name);
    }

}

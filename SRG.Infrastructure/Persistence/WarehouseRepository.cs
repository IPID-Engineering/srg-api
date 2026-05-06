using Microsoft.EntityFrameworkCore;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Infrastructure.Persistence;

public class WarehouseRepository(AppDbContext dbContext) : IWarehouseRepository
{
    public Task<List<Material>> GetMaterialsAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Materials.Include(material => material.Category).OrderBy(material => material.Name).ToListAsync(cancellationToken);
    }

    public Task<Material?> GetMaterialByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Materials.Include(material => material.Category).FirstOrDefaultAsync(material => material.Id == id, cancellationToken);
    }

    public async Task AddMaterialAsync(Material material, CancellationToken cancellationToken = default)
    {
        await dbContext.Materials.AddAsync(material, cancellationToken);
    }

    public void RemoveMaterial(Material material)
    {
        dbContext.Materials.Remove(material);
    }

    public async Task<bool> IsMaterialInUseAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        var inStock = await dbContext.WarehouseStocks.AnyAsync(s => s.MaterialId == materialId && s.Quantity > 0, cancellationToken);
        if (inStock) return true;

        var inOrderedMaterials = await dbContext.Set<OrderedMaterial>().AnyAsync(om => om.MaterialId == materialId, cancellationToken);
        if (inOrderedMaterials) return true;

        var inMaterialUsages = await dbContext.MaterialUsages.AnyAsync(mu => mu.MaterialId == materialId, cancellationToken);
        if (inMaterialUsages) return true;

        var inGrvItems = await dbContext.Set<GoodsReceivedVoucherItem>().AnyAsync(gi => gi.MaterialId == materialId, cancellationToken);
        if (inGrvItems) return true;

        var inIssueItems = await dbContext.Set<IssueItem>().AnyAsync(ii => ii.MaterialId == materialId, cancellationToken);
        if (inIssueItems) return true;

        return false;
    }

    public Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Categories
            .Include(category => category.ParentCategory)
            .Include(category => category.SubCategories)
            .Include(category => category.Materials)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Categories
            .Include(category => category.ParentCategory)
            .Include(category => category.SubCategories)
            .Include(category => category.Materials)
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
    }

    public Task<Category?> GetCategoryByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return dbContext.Categories.FirstOrDefaultAsync(category => category.Name == name, cancellationToken);
    }

    public Task<Category?> GetCategoryByCodesAsync(string familyCode, string? subFamilyCode, CancellationToken cancellationToken = default)
    {
        return dbContext.Categories.FirstOrDefaultAsync(
            category => category.FamilyCode == familyCode && category.SubFamilyCode == subFamilyCode,
            cancellationToken);
    }

    public async Task<Dictionary<Guid, decimal>> GetCategoryUsageTotalsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.MaterialUsages
            .Include(mu => mu.Material)
            .GroupBy(mu => mu.Material!.CategoryId)
            .Select(g => new { CategoryId = g.Key, TotalUsage = g.Sum(mu => mu.Quantity) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.TotalUsage, cancellationToken);
    }

    public async Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        await dbContext.Categories.AddAsync(category, cancellationToken);
    }

    public void RemoveCategory(Category category)
    {
        dbContext.Categories.Remove(category);
    }

    public async Task<Category> GetOrCreateImportCategoryAsync(CancellationToken cancellationToken = default)
    {
        const string importCategoryName = "Import GRV";
        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Name == importCategoryName, cancellationToken);
        if (category == null)
        {
            category = new Category
            {
                Name = importCategoryName,
                ParentCategoryId = null,
                CreatedAt = DateTime.UtcNow
            };
            await dbContext.Categories.AddAsync(category, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return category;
    }

    public Task<Warehouse?> GetMainWarehouseAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Warehouses.FirstOrDefaultAsync(warehouse => warehouse.Type == WarehouseType.Main, cancellationToken);
    }

    public Task<Warehouse?> GetWarehouseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Warehouses.FirstOrDefaultAsync(warehouse => warehouse.Id == id, cancellationToken);
    }

    public Task<Warehouse?> GetSubWarehouseByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return dbContext.Warehouses.FirstOrDefaultAsync(
            warehouse => warehouse.Type == WarehouseType.Sub && warehouse.OwnerId == ownerId,
            cancellationToken);
    }

    public Task<List<Warehouse>> GetAllSubWarehousesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Warehouses
            .Where(warehouse => warehouse.Type == WarehouseType.Sub)
            .OrderBy(warehouse => warehouse.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddWarehouseAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        await dbContext.Warehouses.AddAsync(warehouse, cancellationToken);
    }

    public Task<List<WarehouseStock>> GetStockAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return dbContext.WarehouseStocks
            .Include(s => s.Material)
            .ThenInclude(m => m!.Category)
            .ThenInclude(c => c!.ParentCategory)
            .Where(stock => stock.WarehouseId == warehouseId)
            .OrderBy(stock => stock.Material!.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<WarehouseStock?> GetStockItemAsync(Guid warehouseId, Guid materialId, CancellationToken cancellationToken = default)
    {
        return dbContext.WarehouseStocks.FirstOrDefaultAsync(
            stock => stock.WarehouseId == warehouseId && stock.MaterialId == materialId,
            cancellationToken);
    }

    public async Task AddStockAsync(WarehouseStock stock, CancellationToken cancellationToken = default)
    {
        await dbContext.WarehouseStocks.AddAsync(stock, cancellationToken);
    }

    public Task<List<StockMovement>> GetMovementsAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return dbContext.StockMovements
            .Include(movement => movement.Material)
            .Where(movement => movement.WarehouseId == warehouseId)
            .OrderByDescending(movement => movement.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        await dbContext.StockMovements.AddAsync(movement, cancellationToken);
    }

    public Task<List<Issue>> GetIssuesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Issues
            .Include(issue => issue.WorkOrder)
            .Include(issue => issue.ToWarehouse)
            .Include(issue => issue.Items)
            .ThenInclude(item => item.Material)
            .OrderByDescending(issue => issue.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Issue>> GetIssuesByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        return dbContext.Issues
            .Include(issue => issue.WorkOrder)
            .Include(issue => issue.ToWarehouse)
            .Include(issue => issue.Items)
            .ThenInclude(item => item.Material)
            .Where(issue => issue.WorkOrderId == workOrderId)
            .OrderByDescending(issue => issue.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Issue?> GetIssueByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Issues
            .Include(issue => issue.WorkOrder)
            .Include(issue => issue.Items)
            .ThenInclude(item => item.Material)
            .FirstOrDefaultAsync(issue => issue.Id == id, cancellationToken);
    }

    public async Task AddIssueAsync(Issue issue, CancellationToken cancellationToken = default)
    {
        await dbContext.Issues.AddAsync(issue, cancellationToken);
    }

    public async Task AddIssueItemAsync(IssueItem item, CancellationToken cancellationToken = default)
    {
        await dbContext.IssueItems.AddAsync(item, cancellationToken);
    }

    public Task<Return?> GetReturnByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Returns
            .Include(returnDoc => returnDoc.Items)
            .ThenInclude(item => item.Material)
            .FirstOrDefaultAsync(returnDoc => returnDoc.Id == id, cancellationToken);
    }

    public Task<List<Return>> GetReturnsByStatusAsync(ReturnStatus status, CancellationToken cancellationToken = default)
    {
        return dbContext.Returns
            .Include(returnDoc => returnDoc.Items)
            .ThenInclude(item => item.Material)
            .Where(returnDoc => returnDoc.Status == status)
            .OrderByDescending(returnDoc => returnDoc.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddReturnAsync(Return returnDocument, CancellationToken cancellationToken = default)
    {
        await dbContext.Returns.AddAsync(returnDocument, cancellationToken);
    }

    public async Task AddReturnItemAsync(ReturnItem item, CancellationToken cancellationToken = default)
    {
        await dbContext.ReturnItems.AddAsync(item, cancellationToken);
    }

    public Task<GoodsReceivedVoucher?> GetGoodsReceivedVoucherByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.GoodsReceivedVouchers
            .Include(grv => grv.Items)
            .ThenInclude(item => item.Material)
            .FirstOrDefaultAsync(grv => grv.Id == id, cancellationToken);
    }

    public Task<GoodsReceivedVoucher?> GetGoodsReceivedVoucherByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return dbContext.GoodsReceivedVouchers.FirstOrDefaultAsync(grv => grv.Number == number, cancellationToken);
    }

    public Task<List<GoodsReceivedVoucher>> GetGoodsReceivedVouchersAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.GoodsReceivedVouchers
            .Include(grv => grv.Items)
            .ThenInclude(item => item.Material)
            .OrderByDescending(grv => grv.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddGoodsReceivedVoucherAsync(GoodsReceivedVoucher voucher, CancellationToken cancellationToken = default)
    {
        await dbContext.GoodsReceivedVouchers.AddAsync(voucher, cancellationToken);
    }

    public async Task AddGoodsReceivedVoucherItemAsync(GoodsReceivedVoucherItem item, CancellationToken cancellationToken = default)
    {
        await dbContext.GoodsReceivedVoucherItems.AddAsync(item, cancellationToken);
    }

    public void RemoveGoodsReceivedVoucher(GoodsReceivedVoucher voucher)
    {
        dbContext.GoodsReceivedVoucherItems.RemoveRange(voucher.Items);
        dbContext.GoodsReceivedVouchers.Remove(voucher);
    }

    public async Task RemoveStockMovementsBySourceAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        var movements = await dbContext.StockMovements
            .Where(m => m.SourceId == sourceId)
            .ToListAsync(cancellationToken);
        dbContext.StockMovements.RemoveRange(movements);
    }

    public Task<List<MaterialRequest>> GetMaterialRequestsByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        return dbContext.MaterialRequests
            .Include(r => r.WorkOrder)
            .Include(r => r.Material)
            .Where(r => r.WorkOrderId == workOrderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<MaterialRequest>> GetPendingMaterialRequestsAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.MaterialRequests
            .Include(r => r.WorkOrder)
            .Include(r => r.Material)
            .Where(r => r.Status == MaterialRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<MaterialRequest?> GetMaterialRequestByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.MaterialRequests
            .Include(r => r.WorkOrder)
            .Include(r => r.Material)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddMaterialRequestAsync(MaterialRequest request, CancellationToken cancellationToken = default)
    {
        await dbContext.MaterialRequests.AddAsync(request, cancellationToken);
    }

    public void RemoveMaterialRequest(MaterialRequest request)
    {
        dbContext.MaterialRequests.Remove(request);
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await operation();
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

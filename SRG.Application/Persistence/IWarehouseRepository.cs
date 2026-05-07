using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.Persistence;

public interface IWarehouseRepository
{
    Task<List<Material>> GetMaterialsAsync(CancellationToken cancellationToken = default);
    Task<Material?> GetMaterialByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddMaterialAsync(Material material, CancellationToken cancellationToken = default);
    void RemoveMaterial(Material material);
    Task<bool> IsMaterialInUseAsync(Guid materialId, CancellationToken cancellationToken = default);

    Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryByCodesAsync(string familyCode, string? subFamilyCode, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, decimal>> GetCategoryUsageTotalsAsync(CancellationToken cancellationToken = default);
    Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default);
    void RemoveCategory(Category category);
    Task<Category> GetOrCreateImportCategoryAsync(CancellationToken cancellationToken = default);

    Task<Warehouse?> GetMainWarehouseAsync(CancellationToken cancellationToken = default);
    Task<Warehouse?> GetWarehouseByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetSubWarehouseByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<List<Warehouse>> GetAllSubWarehousesAsync(CancellationToken cancellationToken = default);
    Task AddWarehouseAsync(Warehouse warehouse, CancellationToken cancellationToken = default);

    Task<List<WarehouseStock>> GetStockAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<WarehouseStock?> GetStockItemAsync(Guid warehouseId, Guid materialId, CancellationToken cancellationToken = default);
    Task AddStockAsync(WarehouseStock stock, CancellationToken cancellationToken = default);
    Task<List<StockMovement>> GetMovementsAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);

    Task<List<Issue>> GetIssuesAsync(CancellationToken cancellationToken = default);
    Task<List<Issue>> GetIssuesByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<Issue?> GetIssueByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Issue?> GetIssueBySourceIdAsync(Guid sourceId, CancellationToken cancellationToken = default);
    Task<int> GetNextIssueNumberAsync(CancellationToken cancellationToken = default);
    Task AddIssueAsync(Issue issue, CancellationToken cancellationToken = default);
    Task AddIssueItemAsync(IssueItem item, CancellationToken cancellationToken = default);

    Task<Return?> GetReturnByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Return>> GetReturnsByStatusAsync(ReturnStatus status, CancellationToken cancellationToken = default);
    Task AddReturnAsync(Return returnDocument, CancellationToken cancellationToken = default);
    Task AddReturnItemAsync(ReturnItem item, CancellationToken cancellationToken = default);

    Task<GoodsReceivedVoucher?> GetGoodsReceivedVoucherByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GoodsReceivedVoucher?> GetGoodsReceivedVoucherByNumberAsync(string number, CancellationToken cancellationToken = default);
    Task<List<GoodsReceivedVoucher>> GetGoodsReceivedVouchersAsync(CancellationToken cancellationToken = default);
    Task AddGoodsReceivedVoucherAsync(GoodsReceivedVoucher voucher, CancellationToken cancellationToken = default);
    Task AddGoodsReceivedVoucherItemAsync(GoodsReceivedVoucherItem item, CancellationToken cancellationToken = default);
    void RemoveGoodsReceivedVoucher(GoodsReceivedVoucher voucher);
    Task RemoveStockMovementsBySourceAsync(Guid sourceId, CancellationToken cancellationToken = default);

    Task<List<MaterialRequest>> GetMaterialRequestsByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<List<MaterialRequest>> GetPendingMaterialRequestsAsync(CancellationToken cancellationToken = default);
    Task<MaterialRequest?> GetMaterialRequestByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddMaterialRequestAsync(MaterialRequest request, CancellationToken cancellationToken = default);
    void RemoveMaterialRequest(MaterialRequest request);

    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

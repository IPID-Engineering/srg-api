namespace SRG.Application.Warehouses;

public interface IWarehouseService
{
    Task<WarehouseResponse> GetMainWarehouseAsync(CancellationToken cancellationToken = default);
    Task<WarehouseResponse> GetSubWarehouseAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<WarehouseResponse> GetForemanWarehouseAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<WarehouseResponse>> GetAllSubWarehousesAsync(CancellationToken cancellationToken = default);
    Task<List<StockResponse>> GetStockAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<List<StockMovementResponse>> GetMovementsAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}

namespace SRG.Application.Warehouses;

public interface IGoodsReceivedVoucherService
{
    Task<GoodsReceivedVoucherResponse> CreateAsync(
        CreateGoodsReceivedVoucherRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default);

    Task<List<GoodsReceivedVoucherResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GoodsReceivedVoucherResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GoodsReceivedVoucherResponse> AddItemAsync(Guid id, AddGoodsReceivedVoucherItemRequest request, CancellationToken cancellationToken = default);
    Task<GoodsReceivedVoucherResponse> ConfirmAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<GoodsReceivedVoucherResponse> CancelAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<GoodsReceivedVoucherResponse> ImportAsync(ImportGrvRequest request, Guid createdById, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}

namespace SRG.Application.Warehouses;

public interface IReturnService
{
    Task<ReturnResponse> CreateReturnAsync(Guid foremanId, CancellationToken cancellationToken = default);
    Task<ReturnResponse> AddItemAsync(Guid returnId, AddReturnItemRequest request, CancellationToken cancellationToken = default);
    Task<ReturnResponse> SubmitAsync(Guid returnId, CancellationToken cancellationToken = default);
    Task<ReturnResponse> ApproveAsync(Guid returnId, CancellationToken cancellationToken = default);
    Task<List<ReturnResponse>> GetSubmittedAsync(CancellationToken cancellationToken = default);
}

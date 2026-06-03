namespace SRG.Application.Warehouses;

public interface IIssueService
{
    Task<List<IssueResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<IssueResponse>> GetByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<IssueResponse> CreateIssueAsync(CreateIssueRequest request, Guid createdById, CancellationToken cancellationToken = default);
    Task<IssueResponse> AddItemAsync(Guid issueId, AddIssueItemRequest request, CancellationToken cancellationToken = default);
    Task<IssueResponse> ConfirmIssueAsync(Guid issueId, ConfirmIssueRequest request, CancellationToken cancellationToken = default);
    Task<List<IssueWorkerOption>> GetWorkersForIssueAsync(Guid issueId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateProtocolAsync(Guid issueId, CancellationToken cancellationToken = default);
    Task<IssueVerificationResponse> VerifyIssueAsync(Guid issueId, string code, CancellationToken cancellationToken = default);
}

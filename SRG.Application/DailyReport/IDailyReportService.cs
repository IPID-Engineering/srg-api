namespace SRG.Application.DailyReports;

public interface IDailyReportService
{
    Task<DailyReportResponse> CreateDailyReportAsync(
        CreateDailyReportRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default);

    Task<List<DailyReportResponse>> GetByCrewAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task<List<DailyReportResponse>> GetByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<List<DailyReportResponse>> GetSubmittedAsync(CancellationToken cancellationToken = default);
    Task<List<DailyReportResponse>> GetForPmReviewAsync(CancellationToken cancellationToken = default);
    Task<List<DailyReportResponse>> GetForSpmReviewAsync(CancellationToken cancellationToken = default);
    Task<List<DailyReportResponse>> GetForSubcontractorReviewAsync(CancellationToken cancellationToken = default);
    Task<List<DailyReportCalendarResponse>> GetCalendarAsync(Guid crewId, int year, int month, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> UpdateNotesAsync(Guid id, UpdateDailyReportNotesRequest request, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> UpdateWorkOrderAsync(Guid id, UpdateDailyReportWorkOrderRequest request, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> AddWorkOrderAsync(Guid id, AddDailyReportWorkOrderRequest request, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> RemoveWorkOrderAsync(Guid id, Guid workOrderId, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> AddWorkHoursAsync(Guid id, AddWorkHourRequest request, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> AddWorkAsync(Guid id, AddWorkEntryRequest request, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> AddMaterialAsync(
        Guid id,
        AddMaterialUsageRequest request,
        Guid foremanId,
        CancellationToken cancellationToken = default);
    Task<DailyReportResponse> SubmitDailyReportAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> RejectDailyReportAsync(Guid id, RejectDailyReportRequest request, CancellationToken cancellationToken = default);

    Task<DailyReportResponse> AddCommentAsync(Guid id, AddDailyReportCommentRequest request, Guid authorId, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> ResolveCommentAsync(Guid id, Guid commentId, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> UnresolveCommentAsync(Guid id, Guid commentId, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> SendBackToForemanAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> PmApproveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> SpmApproveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DailyReportResponse> SubcontractorApproveAsync(Guid id, CancellationToken cancellationToken = default);
}

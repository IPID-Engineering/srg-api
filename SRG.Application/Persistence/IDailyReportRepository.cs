using SRG.Application.DailyReports;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.Persistence;

public interface IDailyReportRepository
{
    Task<Domain.Entities.DailyReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Domain.Entities.DailyReport?> GetByIdWithWorkersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Domain.Entities.DailyReport>> GetByCrewAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task<List<Domain.Entities.DailyReport>> GetByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<List<Domain.Entities.DailyReport>> GetByCrewDateRangeAsync(Guid crewId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<List<Domain.Entities.DailyReport>> GetByCrewForStatsAsync(Guid crewId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<List<Domain.Entities.DailyReport>> GetByStatusAsync(DailyReportStatus status, CancellationToken cancellationToken = default);
    Task<List<Domain.Entities.DailyReport>> GetByStatusesAsync(IEnumerable<DailyReportStatus> statuses, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns lightweight list items with aggregated data using SQL projection.
    /// Much faster than GetByStatusesAsync for list views.
    /// </summary>
    Task<List<DailyReportListItemResponse>> GetListItemsByStatusesAsync(IEnumerable<DailyReportStatus> statuses, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns lightweight calendar items using SQL projection.
    /// Much faster than GetByCrewDateRangeAsync for calendar views.
    /// </summary>
    Task<List<ForemanDkpCalendarItem>> GetCalendarItemsAsync(Guid crewId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsForCrewDateAsync(Guid crewId, DateOnly date, CancellationToken cancellationToken = default);
    Task<Domain.Entities.DailyReport?> GetBySubcontractorCrewAndDateAsync(Guid subcontractorCrewId, DateOnly date, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Entities.DailyReport dailyReport, CancellationToken cancellationToken = default);
    Task AddWorkHoursAsync(WorkHour entry, CancellationToken cancellationToken = default);
    Task AddWorkEntryAsync(WorkEntry entry, CancellationToken cancellationToken = default);
    Task AddMaterialAsync(MaterialUsage entry, CancellationToken cancellationToken = default);
    Task AddCommentAsync(DailyReportComment comment, CancellationToken cancellationToken = default);
    Task AddStatusHistoryAsync(DailyReportStatusHistory history, CancellationToken cancellationToken = default);
    Task AddChangeHistoryAsync(DailyReportChangeHistory history, CancellationToken cancellationToken = default);
    Task AddDailyReportWorkOrderAsync(DailyReportWorkOrder entry, CancellationToken cancellationToken = default);
    Task RemoveDailyReportWorkOrderAsync(DailyReportWorkOrder entry, CancellationToken cancellationToken = default);
    
    // Delete operations
    Task<List<Domain.Entities.DailyReport>> GetBySubcontractorCrewAsync(Guid subcontractorCrewId, CancellationToken cancellationToken = default);
    void RemoveDailyReport(Domain.Entities.DailyReport dailyReport);
    void RemoveDailyReports(IEnumerable<Domain.Entities.DailyReport> dailyReports);
    Task RemoveWorkHoursBySubcontractorWorkerAsync(Guid subcontractorWorkerId, CancellationToken cancellationToken = default);
    Task<int> CountDailyReportsBySubcontractorCrewAsync(Guid subcontractorCrewId, CancellationToken cancellationToken = default);
    Task<int> CountWorkHoursBySubcontractorWorkerAsync(Guid subcontractorWorkerId, CancellationToken cancellationToken = default);
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

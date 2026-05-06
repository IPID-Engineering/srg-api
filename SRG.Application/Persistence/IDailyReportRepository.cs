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
    Task<bool> ExistsForCrewDateAsync(Guid crewId, DateOnly date, CancellationToken cancellationToken = default);
    Task<Domain.Entities.DailyReport?> GetBySubcontractorCrewAndDateAsync(Guid subcontractorCrewId, DateOnly date, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Entities.DailyReport dailyReport, CancellationToken cancellationToken = default);
    Task AddWorkHoursAsync(WorkHour entry, CancellationToken cancellationToken = default);
    Task AddWorkEntryAsync(WorkEntry entry, CancellationToken cancellationToken = default);
    Task AddMaterialAsync(MaterialUsage entry, CancellationToken cancellationToken = default);
    Task AddCommentAsync(DailyReportComment comment, CancellationToken cancellationToken = default);
    Task AddStatusHistoryAsync(DailyReportStatusHistory history, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

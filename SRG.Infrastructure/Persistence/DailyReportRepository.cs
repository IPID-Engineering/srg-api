using Microsoft.EntityFrameworkCore;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Infrastructure.Persistence;

public class DailyReportRepository(AppDbContext dbContext) : IDailyReportRepository
{
    public Task<DailyReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DailyReportQuery()
            .FirstOrDefaultAsync(dailyReport => dailyReport.Id == id, cancellationToken);
    }

    public Task<List<DailyReport>> GetByCrewAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        return DailyReportQuery()
            .Where(dailyReport => dailyReport.CrewId == crewId || dailyReport.SubcontractorCrewId == crewId)
            .OrderByDescending(dailyReport => dailyReport.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<List<DailyReport>> GetByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        return DailyReportQuery()
            .Where(dailyReport => dailyReport.WorkOrderId == workOrderId)
            .OrderByDescending(dailyReport => dailyReport.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<List<DailyReport>> GetByCrewDateRangeAsync(
        Guid crewId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return DailyReportQuery()
            .Where(dailyReport => (dailyReport.CrewId == crewId || dailyReport.SubcontractorCrewId == crewId) && dailyReport.Date >= startDate && dailyReport.Date <= endDate)
            .OrderBy(dailyReport => dailyReport.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<List<DailyReport>> GetByStatusAsync(DailyReportStatus status, CancellationToken cancellationToken = default)
    {
        return DailyReportQuery()
            .Where(dailyReport => dailyReport.Status == status)
            .OrderByDescending(dailyReport => dailyReport.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsForCrewDateAsync(Guid crewId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return dbContext.DailyReports.AnyAsync(
            dailyReport => (dailyReport.CrewId == crewId || dailyReport.SubcontractorCrewId == crewId) && dailyReport.Date == date,
            cancellationToken);
    }

    public Task<DailyReport?> GetBySubcontractorCrewAndDateAsync(Guid subcontractorCrewId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return DailyReportWithWorkersQuery()
            .FirstOrDefaultAsync(r => r.SubcontractorCrewId == subcontractorCrewId && r.Date == date, cancellationToken);
    }

    public Task<DailyReport?> GetByIdWithWorkersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DailyReportWithWorkersQuery()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(DailyReport dailyReport, CancellationToken cancellationToken = default)
    {
        await dbContext.DailyReports.AddAsync(dailyReport, cancellationToken);
    }

    public async Task AddWorkHoursAsync(WorkHour entry, CancellationToken cancellationToken = default)
    {
        await dbContext.WorkHours.AddAsync(entry, cancellationToken);
    }

    public async Task AddWorkEntryAsync(WorkEntry entry, CancellationToken cancellationToken = default)
    {
        await dbContext.DailyReportWorkEntries.AddAsync(entry, cancellationToken);
    }

    public async Task AddMaterialAsync(MaterialUsage entry, CancellationToken cancellationToken = default)
    {
        await dbContext.MaterialUsages.AddAsync(entry, cancellationToken);
    }

    public async Task AddCommentAsync(DailyReportComment comment, CancellationToken cancellationToken = default)
    {
        await dbContext.DailyReportComments.AddAsync(comment, cancellationToken);
    }

    public async Task AddStatusHistoryAsync(DailyReportStatusHistory history, CancellationToken cancellationToken = default)
    {
        await dbContext.DailyReportStatusHistory.AddAsync(history, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<DailyReport>> GetByCrewForStatsAsync(
        Guid crewId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return dbContext.DailyReports
            .Include(dr => dr.Crew)
                .ThenInclude(c => c!.Project)
            .Include(dr => dr.SubcontractorCrew)
            .Include(dr => dr.WorkHours)
                .ThenInclude(wh => wh.Worker)
            .Include(dr => dr.WorkHours)
                .ThenInclude(wh => wh.SubcontractorWorker)
            .Include(dr => dr.WorkEntries)
                .ThenInclude(we => we.WorkType)
            .Include(dr => dr.MaterialUsages)
                .ThenInclude(mu => mu.Material)
            .Where(dr => (dr.CrewId == crewId || dr.SubcontractorCrewId == crewId) && dr.Date >= startDate && dr.Date <= endDate)
            .OrderByDescending(dr => dr.Date)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<DailyReport> DailyReportQuery()
    {
        return dbContext.DailyReports
            .Include(dailyReport => dailyReport.Crew)
            .Include(dailyReport => dailyReport.SubcontractorCrew)
            .Include(dailyReport => dailyReport.WorkHours)
            .ThenInclude(wh => wh.Worker)
            .Include(dailyReport => dailyReport.WorkHours)
            .ThenInclude(wh => wh.SubcontractorWorker)
            .Include(dailyReport => dailyReport.WorkEntries)
            .ThenInclude(entry => entry.WorkType)
            .Include(dailyReport => dailyReport.WorkEntries)
            .ThenInclude(entry => entry.OrderedWork)
            .Include(dailyReport => dailyReport.MaterialUsages)
            .ThenInclude(entry => entry.Material)
            .Include(dailyReport => dailyReport.MaterialUsages)
            .ThenInclude(entry => entry.OrderedMaterial)
            .Include(dailyReport => dailyReport.Comments)
            .ThenInclude(comment => comment.Author)
            .Include(dailyReport => dailyReport.Comments)
            .ThenInclude(comment => comment.SubcontractorWorker)
            .Include(dailyReport => dailyReport.Comments)
            .ThenInclude(comment => comment.Replies)
            .ThenInclude(reply => reply.Author)
            .Include(dailyReport => dailyReport.Comments)
            .ThenInclude(comment => comment.Replies)
            .ThenInclude(reply => reply.SubcontractorWorker)
            .Include(dailyReport => dailyReport.StatusHistory)
            .ThenInclude(history => history.ChangedBy);
    }

    private IQueryable<DailyReport> DailyReportWithWorkersQuery()
    {
        return dbContext.DailyReports
            .Include(r => r.WorkOrder)
            .Include(r => r.WorkHours).ThenInclude(wh => wh.Worker)
            .Include(r => r.WorkHours).ThenInclude(wh => wh.SubcontractorWorker)
            .Include(r => r.WorkEntries).ThenInclude(we => we.WorkType)
            .Include(r => r.MaterialUsages).ThenInclude(mu => mu.Material)
            .Include(r => r.Comments).ThenInclude(c => c.Author)
            .Include(r => r.Comments).ThenInclude(c => c.SubcontractorWorker);
    }
}

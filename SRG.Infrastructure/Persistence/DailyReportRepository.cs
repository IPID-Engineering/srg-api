using Microsoft.EntityFrameworkCore;
using SRG.Application.DailyReports;
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

    public Task<List<ForemanDkpCalendarItem>> GetCalendarItemsAsync(
        Guid crewId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return dbContext.DailyReports
            .Where(dr => (dr.CrewId == crewId || dr.SubcontractorCrewId == crewId) && dr.Date >= startDate && dr.Date <= endDate)
            .OrderBy(dr => dr.Date)
            .Select(dr => new ForemanDkpCalendarItem
            {
                Id = dr.Id,
                Date = dr.Date,
                Status = dr.Status.ToString(),
                TotalHours = dr.WorkHours.Sum(wh => wh.Hours),
                HasWork = dr.WorkHours.Count > 0
            })
            .ToListAsync(cancellationToken);
    }

    public Task<List<DailyReport>> GetByStatusAsync(DailyReportStatus status, CancellationToken cancellationToken = default)
    {
        return DailyReportQuery()
            .Where(dailyReport => dailyReport.Status == status)
            .OrderByDescending(dailyReport => dailyReport.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<List<DailyReport>> GetByStatusesAsync(IEnumerable<DailyReportStatus> statuses, CancellationToken cancellationToken = default)
    {
        var statusList = statuses.ToList();
        return DailyReportQuery()
            .Where(dailyReport => statusList.Contains(dailyReport.Status))
            .OrderByDescending(dailyReport => dailyReport.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<List<DailyReportListItemResponse>> GetListItemsByStatusesAsync(IEnumerable<DailyReportStatus> statuses, CancellationToken cancellationToken = default)
    {
        var statusList = statuses.ToList();
        return dbContext.DailyReports
            .Where(dr => statusList.Contains(dr.Status))
            .OrderByDescending(dr => dr.Date)
            .Select(dr => new DailyReportListItemResponse(
                dr.Id,
                dr.Date,
                dr.SubcontractorCrewId,
                dr.Crew != null ? dr.Crew.Name : null,
                dr.SubcontractorCrew != null ? dr.SubcontractorCrew.Name : null,
                dr.Status,
                dr.WorkHours.Sum(wh => wh.Hours),
                dr.WorkEntries.Count,
                dr.MaterialUsages.Count,
                dr.Comments.Any(c => !c.IsResolved && c.ParentCommentId == null),
                dr.StatusHistory
                    .Where(h => h.ToStatus == DailyReportStatus.Rejected)
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => (DailyReportStatus?)h.FromStatus)
                    .FirstOrDefault()))
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

    public async Task AddChangeHistoryAsync(DailyReportChangeHistory history, CancellationToken cancellationToken = default)
    {
        await dbContext.DailyReportChangeHistory.AddAsync(history, cancellationToken);
    }

    public async Task AddDailyReportWorkOrderAsync(DailyReportWorkOrder entry, CancellationToken cancellationToken = default)
    {
        await dbContext.DailyReportWorkOrders.AddAsync(entry, cancellationToken);
    }

    public Task RemoveDailyReportWorkOrderAsync(DailyReportWorkOrder entry, CancellationToken cancellationToken = default)
    {
        dbContext.DailyReportWorkOrders.Remove(entry);
        return Task.CompletedTask;
    }

    public Task<List<DailyReport>> GetBySubcontractorCrewAsync(Guid subcontractorCrewId, CancellationToken cancellationToken = default)
    {
        return dbContext.DailyReports
            .Where(dr => dr.SubcontractorCrewId == subcontractorCrewId)
            .ToListAsync(cancellationToken);
    }

    public void RemoveDailyReport(DailyReport dailyReport)
    {
        dbContext.DailyReports.Remove(dailyReport);
    }

    public void RemoveDailyReports(IEnumerable<DailyReport> dailyReports)
    {
        dbContext.DailyReports.RemoveRange(dailyReports);
    }

    public async Task RemoveWorkHoursBySubcontractorWorkerAsync(Guid subcontractorWorkerId, CancellationToken cancellationToken = default)
    {
        var workHours = await dbContext.WorkHours
            .Where(wh => wh.SubcontractorWorkerId == subcontractorWorkerId)
            .ToListAsync(cancellationToken);
        
        dbContext.WorkHours.RemoveRange(workHours);
    }

    public Task<int> CountDailyReportsBySubcontractorCrewAsync(Guid subcontractorCrewId, CancellationToken cancellationToken = default)
    {
        return dbContext.DailyReports
            .CountAsync(dr => dr.SubcontractorCrewId == subcontractorCrewId, cancellationToken);
    }

    public Task<int> CountWorkHoursBySubcontractorWorkerAsync(Guid subcontractorWorkerId, CancellationToken cancellationToken = default)
    {
        return dbContext.WorkHours
            .CountAsync(wh => wh.SubcontractorWorkerId == subcontractorWorkerId, cancellationToken);
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
            .ThenInclude(ow => ow!.WorkOrder)
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
            .ThenInclude(history => history.ChangedBy)
            .Include(dailyReport => dailyReport.ChangeHistory)
            .ThenInclude(history => history.ChangedBy)
            .Include(dailyReport => dailyReport.DailyReportWorkOrders)
            .ThenInclude(drwo => drwo.WorkOrder)
            .ThenInclude(wo => wo!.OrderedWorks)
            .ThenInclude(ow => ow.WorkType);
    }

    private IQueryable<DailyReport> DailyReportWithWorkersQuery()
    {
        return dbContext.DailyReports
            .Include(r => r.WorkOrder)
            .Include(r => r.WorkHours).ThenInclude(wh => wh.Worker)
            .Include(r => r.WorkHours).ThenInclude(wh => wh.SubcontractorWorker)
            .Include(r => r.WorkEntries).ThenInclude(we => we.WorkType)
            .Include(r => r.WorkEntries).ThenInclude(we => we.OrderedWork).ThenInclude(ow => ow!.WorkOrder)
            .Include(r => r.MaterialUsages).ThenInclude(mu => mu.Material)
            .Include(r => r.Comments).ThenInclude(c => c.Author)
            .Include(r => r.Comments).ThenInclude(c => c.SubcontractorWorker)
            .Include(r => r.Comments).ThenInclude(c => c.Replies).ThenInclude(reply => reply.Author)
            .Include(r => r.Comments).ThenInclude(c => c.Replies).ThenInclude(reply => reply.SubcontractorWorker)
            .Include(r => r.StatusHistory)
            .Include(r => r.DailyReportWorkOrders).ThenInclude(drwo => drwo.WorkOrder).ThenInclude(wo => wo!.OrderedWorks).ThenInclude(ow => ow.WorkType);
    }
}

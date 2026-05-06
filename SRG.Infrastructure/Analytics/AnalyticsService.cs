using Microsoft.EntityFrameworkCore;
using SRG.Application.Analytics;
using SRG.Domain.Enums;
using SRG.Infrastructure.Persistence;

namespace SRG.Infrastructure.Analytics;

public class AnalyticsService(AppDbContext dbContext) : IAnalyticsService
{
    public async Task<PMAnalyticsResponse> GetPMAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var dailyReportStats = await dbContext.DailyReports
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new DailyReportStatsResponse(
                group.Count(),
                group.Count(report => report.Status == DailyReportStatus.PmApproved || report.Status == DailyReportStatus.SpmApproved),
                group.Count(report => report.Status == DailyReportStatus.Rejected),
                group.Count(report => report.Status == DailyReportStatus.Submitted || report.Status == DailyReportStatus.PmReview || report.Status == DailyReportStatus.SpmReview)))
            .FirstOrDefaultAsync(cancellationToken)
            ?? new DailyReportStatsResponse(0, 0, 0, 0);

        var workProgressData = await dbContext.DailyReportWorkEntries
            .AsNoTracking()
            .Join(dbContext.DailyReports.AsNoTracking(), work => work.DailyReportId, report => report.Id, (work, report) => new { work, report })
            .Join(dbContext.Projects.AsNoTracking(), joined => joined.report.ProjectId, project => project.Id, (joined, project) => new { joined.work, project })
            .Select(row => new { row.project.Id, row.project.Name, row.work.Quantity })
            .ToListAsync(cancellationToken);

        var workProgress = workProgressData
            .GroupBy(row => new { row.Id, row.Name })
            .Select(group => new WorkProgressByProjectResponse(
                group.Key.Id,
                group.Key.Name,
                group.Count(),
                group.Sum(row => row.Quantity)))
            .OrderByDescending(row => row.TotalQuantity)
            .Take(20)
            .ToList();

        var topCrewsData = await dbContext.DailyReports
            .AsNoTracking()
            .Join(dbContext.Crews.AsNoTracking(), report => report.CrewId, crew => crew.Id, (report, crew) => new { report, crew })
            .Select(row => new { row.crew.Id, row.crew.Name })
            .ToListAsync(cancellationToken);

        var topCrews = topCrewsData
            .GroupBy(row => new { row.Id, row.Name })
            .Select(group => new TopCrewResponse(group.Key.Id, group.Key.Name, group.Count()))
            .OrderByDescending(row => row.DailyReportCount)
            .Take(10)
            .ToList();

        var dailyReportOverTime = await dbContext.DailyReports
            .AsNoTracking()
            .GroupBy(report => report.Date)
            .Select(group => new DailyReportOverTimeResponse(group.Key, group.Count()))
            .OrderByDescending(row => row.Date)
            .Take(30)
            .ToListAsync(cancellationToken);

        dailyReportOverTime = [.. dailyReportOverTime.OrderBy(row => row.Date)];

        return new PMAnalyticsResponse(dailyReportStats, workProgress, topCrews, dailyReportOverTime);
    }

    public async Task<LogisticsAnalyticsResponse> GetLogisticsAnalyticsAsync(
        decimal lowStockThreshold = 10,
        CancellationToken cancellationToken = default)
    {
        var totalMaterials = await dbContext.Materials.AsNoTracking().CountAsync(cancellationToken);

        var materialUsageData = await dbContext.MaterialUsages
            .AsNoTracking()
            .Join(dbContext.Materials.AsNoTracking(), entry => entry.MaterialId, material => material.Id, (entry, material) => new { entry, material })
            .Select(row => new { row.material.Id, row.material.Name, row.entry.Quantity })
            .ToListAsync(cancellationToken);

        var materialUsage = materialUsageData
            .GroupBy(row => new { row.Id, row.Name })
            .Select(group => new MaterialUsageSummaryResponse(group.Key.Id, group.Key.Name, group.Sum(row => row.Quantity)))
            .OrderByDescending(row => row.TotalUsed)
            .Take(20)
            .ToList();

        var lowStockQuery = dbContext.WarehouseStocks
            .AsNoTracking()
            .Where(stock => stock.Quantity < lowStockThreshold);

        var lowStockCount = await lowStockQuery.CountAsync(cancellationToken);

        var lowStock = await lowStockQuery
            .OrderBy(stock => stock.Quantity)
            .Select(stock => new LowStockResponse(
                stock.MaterialId,
                stock.Material!.Name,
                stock.Material.Unit,
                stock.Quantity))
            .Take(50)
            .ToListAsync(cancellationToken);

        var issued = await dbContext.IssueItems
            .AsNoTracking()
            .Where(item => item.Issue!.Status == IssueStatus.Confirmed)
            .SumAsync(item => (decimal?)item.Quantity, cancellationToken) ?? 0;

        var returned = await dbContext.ReturnItems
            .AsNoTracking()
            .Where(item => item.Return!.Status == ReturnStatus.Approved)
            .SumAsync(item => (decimal?)item.Quantity, cancellationToken) ?? 0;

        return new LogisticsAnalyticsResponse(
            totalMaterials,
            lowStockCount,
            materialUsage,
            lowStock,
            new WarehouseFlowResponse(issued, returned));
    }

    public async Task<ForemanAnalyticsResponse> GetForemanAnalyticsAsync(
        Guid foremanId,
        CancellationToken cancellationToken = default)
    {
        var myReports = dbContext.DailyReports
            .AsNoTracking()
            .Where(report => report.CreatedById == foremanId);

        var reportIds = myReports.Select(report => report.Id);

        var totalDkp = await myReports.CountAsync(cancellationToken);
        var totalHours = await dbContext.WorkHours
            .AsNoTracking()
            .Where(entry => reportIds.Contains(entry.DailyReportId))
            .SumAsync(entry => (decimal?)entry.Hours, cancellationToken) ?? 0;
        var totalWorkEntries = await dbContext.DailyReportWorkEntries
            .AsNoTracking()
            .CountAsync(entry => reportIds.Contains(entry.DailyReportId), cancellationToken);

        var hoursOverTimeData = await dbContext.WorkHours
            .AsNoTracking()
            .Join(myReports, entry => entry.DailyReportId, report => report.Id, (entry, report) => new { entry, report })
            .Select(row => new { row.report.Date, row.entry.Hours })
            .ToListAsync(cancellationToken);

        var hoursOverTime = hoursOverTimeData
            .GroupBy(row => row.Date)
            .Select(group => new HoursOverTimeResponse(group.Key, group.Sum(row => row.Hours)))
            .OrderByDescending(row => row.Date)
            .Take(30)
            .ToList();

        hoursOverTime = [.. hoursOverTime.OrderBy(row => row.Date)];

        return new ForemanAnalyticsResponse(totalDkp, totalHours, totalWorkEntries, hoursOverTime);
    }

    public async Task<CrewAnalyticsResponse> GetCrewAnalyticsAsync(
        Guid crewId,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        // Try regular Crew first
        var crew = await dbContext.Crews
            .AsNoTracking()
            .Where(c => c.Id == crewId)
            .Select(c => new { c.Id, c.Name })
            .FirstOrDefaultAsync(cancellationToken);

        string crewName;
        if (crew != null)
        {
            crewName = crew.Name;
        }
        else
        {
            // Try SubcontractorCrew
            var subcontractorCrew = await dbContext.SubcontractorCrews
                .AsNoTracking()
                .Where(c => c.Id == crewId)
                .Select(c => new { c.Id, c.Name })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException("Crew was not found.");
            crewName = subcontractorCrew.Name;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = dateFrom ?? today.AddDays(-30);
        var to = dateTo ?? today;

        return new CrewAnalyticsResponse(
            crewId,
            crewName,
            await GetCrewWorkStatsAsync(crewId, cancellationToken),
            await GetCrewMaterialUsageAsync(crewId, from, to, cancellationToken),
            await GetWorkerStatsAsync(crewId, cancellationToken));
    }

    public async Task<List<CrewMaterialUsageResponse>> GetCrewMaterialUsageAsync(
        Guid crewId,
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken cancellationToken = default)
    {
        if (dateFrom > dateTo)
        {
            throw new ArgumentException("dateFrom cannot be later than dateTo.");
        }

        var usage = await dbContext.MaterialUsages
            .AsNoTracking()
            .Join(dbContext.DailyReports.AsNoTracking(), usage => usage.DailyReportId, report => report.Id, (usage, report) => new { usage, report })
            .Join(dbContext.Materials.AsNoTracking(), row => row.usage.MaterialId, material => material.Id, (row, material) => new { row.usage, row.report, material })
            .Where(row => row.report.CrewId == crewId && row.report.Date >= dateFrom && row.report.Date <= dateTo)
            .GroupBy(row => new { row.material.Id, row.material.Name, row.material.Unit })
            .Select(group => new
            {
                group.Key.Id,
                group.Key.Name,
                group.Key.Unit,
                TotalUsed = group.Sum(row => row.usage.Quantity),
                Days = group.Select(row => row.report.Date).Distinct().Count()
            })
            .OrderByDescending(row => row.TotalUsed)
            .ToListAsync(cancellationToken);

        return usage
            .Select(row => new CrewMaterialUsageResponse(
                row.Id,
                row.Name,
                row.Unit,
                row.TotalUsed,
                row.Days == 0 ? 0 : decimal.Round(row.TotalUsed / row.Days, 2)))
            .ToList();
    }

    public async Task<List<CrewMaterialUsageResponse>> GetCrewMaterialAverageAsync(
        Guid crewId,
        CancellationToken cancellationToken = default)
    {
        var usage = await dbContext.MaterialUsages
            .AsNoTracking()
            .Join(dbContext.DailyReports.AsNoTracking(), usage => usage.DailyReportId, report => report.Id, (usage, report) => new { usage, report })
            .Join(dbContext.Materials.AsNoTracking(), row => row.usage.MaterialId, material => material.Id, (row, material) => new { row.usage, row.report, material })
            .Where(row => row.report.CrewId == crewId)
            .GroupBy(row => new { row.material.Id, row.material.Name, row.material.Unit })
            .Select(group => new
            {
                group.Key.Id,
                group.Key.Name,
                group.Key.Unit,
                TotalUsed = group.Sum(row => row.usage.Quantity),
                Days = group.Select(row => row.report.Date).Distinct().Count()
            })
            .OrderByDescending(row => row.TotalUsed)
            .ToListAsync(cancellationToken);

        return usage
            .Select(row => new CrewMaterialUsageResponse(
                row.Id,
                row.Name,
                row.Unit,
                row.TotalUsed,
                row.Days == 0 ? 0 : decimal.Round(row.TotalUsed / row.Days, 2)))
            .ToList();
    }

    public async Task<CrewWorkStatsResponse> GetCrewWorkStatsAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        var reportIds = dbContext.DailyReports
            .AsNoTracking()
            .Where(report => report.CrewId == crewId)
            .Select(report => report.Id);

        var totalWorkEntries = await dbContext.DailyReportWorkEntries
            .AsNoTracking()
            .CountAsync(entry => reportIds.Contains(entry.DailyReportId), cancellationToken);

        var totalHours = await dbContext.WorkHours
            .AsNoTracking()
            .Where(hour => reportIds.Contains(hour.DailyReportId))
            .SumAsync(hour => (decimal?)hour.Hours, cancellationToken) ?? 0;

        var daysWithHours = await dbContext.WorkHours
            .AsNoTracking()
            .Join(dbContext.DailyReports.AsNoTracking(), hour => hour.DailyReportId, report => report.Id, (hour, report) => new { hour, report })
            .Where(row => row.report.CrewId == crewId)
            .Select(row => row.report.Date)
            .Distinct()
            .CountAsync(cancellationToken);

        return new CrewWorkStatsResponse(
            totalWorkEntries,
            totalHours,
            daysWithHours == 0 ? 0 : decimal.Round(totalHours / daysWithHours, 2));
    }

    public async Task<List<WorkerStatsResponse>> GetWorkerStatsAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        var stats = await dbContext.Workers
            .AsNoTracking()
            .Where(worker => worker.CrewId == crewId)
            .GroupJoin(
                dbContext.WorkHours
                    .AsNoTracking()
                    .Join(dbContext.DailyReports.AsNoTracking(), hour => hour.DailyReportId, report => report.Id, (hour, report) => new { hour, report })
                    .Where(row => row.report.CrewId == crewId),
                worker => worker.Id,
                row => row.hour.WorkerId,
                (worker, hours) => new
                {
                    WorkerId = worker.Id,
                    WorkerName = worker.FirstName + " " + worker.LastName,
                    TotalHours = hours.Sum(row => (decimal?)row.hour.Hours) ?? 0,
                    DaysWorked = hours.Select(row => row.report.Date).Distinct().Count()
                })
            .OrderByDescending(row => row.TotalHours)
            .ToListAsync(cancellationToken);

        return stats
            .Select(row => new WorkerStatsResponse(
                row.WorkerId,
                row.WorkerName,
                row.TotalHours,
                row.DaysWorked == 0 ? 0 : decimal.Round(row.TotalHours / row.DaysWorked, 2),
                row.DaysWorked))
            .ToList();
    }

    public async Task<MaterialStatsResponse> GetMaterialStatsAsync(
        Guid materialId,
        CancellationToken cancellationToken = default)
    {
        var material = await dbContext.Materials
            .AsNoTracking()
            .Include(m => m.Category)
            .Where(m => m.Id == materialId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Materiał nie został znaleziony.");

        var mainWarehouse = await dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Type == WarehouseType.Main, cancellationToken);

        decimal currentStock = 0;
        if (mainWarehouse != null)
        {
            currentStock = await dbContext.WarehouseStocks
                .AsNoTracking()
                .Where(s => s.WarehouseId == mainWarehouse.Id && s.MaterialId == materialId)
                .Select(s => s.Quantity)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var totalReceived = await dbContext.GoodsReceivedVoucherItems
            .AsNoTracking()
            .Where(i => i.MaterialId == materialId)
            .SumAsync(i => (decimal?)i.Quantity, cancellationToken) ?? 0;

        var totalIssued = await dbContext.IssueItems
            .AsNoTracking()
            .Where(i => i.MaterialId == materialId && i.Issue!.Status == IssueStatus.Confirmed)
            .SumAsync(i => (decimal?)i.Quantity, cancellationToken) ?? 0;

        var totalUsed = await dbContext.MaterialUsages
            .AsNoTracking()
            .Where(u => u.MaterialId == materialId)
            .SumAsync(u => (decimal?)u.Quantity, cancellationToken) ?? 0;

        var usageByCrewRaw = await dbContext.MaterialUsages
            .AsNoTracking()
            .Where(u => u.MaterialId == materialId)
            .Join(dbContext.DailyReports.AsNoTracking(), u => u.DailyReportId, r => r.Id, (u, r) => new { u.Quantity, CrewId = r.CrewId ?? r.SubcontractorCrewId })
            .Where(x => x.CrewId.HasValue)
            .GroupBy(x => x.CrewId!.Value)
            .Select(g => new { CrewId = g.Key, TotalUsed = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.TotalUsed)
            .Take(10)
            .ToListAsync(cancellationToken);

        var crewIds = usageByCrewRaw.Select(x => x.CrewId).Distinct().ToList();
        var crewNames = await dbContext.Crews
            .AsNoTracking()
            .Where(c => crewIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var subcontractorCrewNames = await dbContext.SubcontractorCrews
            .AsNoTracking()
            .Where(c => crewIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var usageByCrew = usageByCrewRaw
            .Select(x => new MaterialUsageByCrewResponse(
                x.CrewId,
                crewNames.TryGetValue(x.CrewId, out var name) ? name : (subcontractorCrewNames.TryGetValue(x.CrewId, out var subName) ? subName : "Nieznana brygada"),
                x.TotalUsed))
            .ToList();

        var usageOverTimeRaw = await dbContext.MaterialUsages
            .AsNoTracking()
            .Where(u => u.MaterialId == materialId)
            .Join(dbContext.DailyReports.AsNoTracking(), u => u.DailyReportId, r => r.Id, (u, r) => new { u.Quantity, r.Date })
            .GroupBy(x => x.Date)
            .Select(g => new { Date = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Date)
            .Take(60)
            .ToListAsync(cancellationToken);

        var usageOverTime = usageOverTimeRaw
            .OrderBy(x => x.Date)
            .Select(x => new MaterialUsageOverTimeResponse(x.Date, x.Quantity))
            .ToList();

        var deliveries = await dbContext.GoodsReceivedVoucherItems
            .AsNoTracking()
            .Where(i => i.MaterialId == materialId)
            .Join(dbContext.GoodsReceivedVouchers.AsNoTracking(), i => i.GoodsReceivedVoucherId, g => g.Id, (i, g) => new { i.Quantity, g.Id, g.Number, g.DeliveryDate, g.SupplierName })
            .OrderByDescending(x => x.DeliveryDate)
            .Take(20)
            .Select(x => new MaterialDeliveryResponse(x.Id, x.Number, x.DeliveryDate, x.Quantity, x.SupplierName))
            .ToListAsync(cancellationToken);

        return new MaterialStatsResponse(
            material.Id,
            material.Name,
            material.Unit,
            material.Category?.Name ?? "Brak kategorii",
            currentStock,
            totalReceived,
            totalIssued,
            totalUsed,
            usageByCrew,
            usageOverTime,
            deliveries);
    }
}

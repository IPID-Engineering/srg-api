using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Analytics;

public interface ICrewStatsService
{
    Task<CrewStatsResponse> GetCrewStatsAsync(CrewStatsRequest request, CancellationToken cancellationToken = default);
}

public class CrewStatsService(
    IDailyReportRepository dailyReportRepository,
    IConstructionRepository constructionRepository) : ICrewStatsService
{
    public async Task<CrewStatsResponse> GetCrewStatsAsync(CrewStatsRequest request, CancellationToken cancellationToken = default)
    {
        // Try to find in regular Crews first
        var crew = await constructionRepository.GetCrewByIdAsync(request.CrewId, cancellationToken);
        
        string crewName;
        string projectName;
        
        if (crew != null)
        {
            crewName = crew.Name;
        }
        else
        {
            // Try SubcontractorCrew
            var subcontractorCrew = await constructionRepository.GetSubcontractorCrewByIdAsync(request.CrewId, cancellationToken)
                ?? throw new KeyNotFoundException("Brygada nie została znaleziona.");
            crewName = subcontractorCrew.Name;
        }

        var dateTo = request.DateTo ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dateFrom = request.DateFrom ?? dateTo.AddMonths(-1);

        var reports = await dailyReportRepository.GetByCrewForStatsAsync(
            request.CrewId, dateFrom, dateTo, cancellationToken);

        projectName = reports.FirstOrDefault()?.Crew?.Project?.Name ?? "—";

        var recentReports = reports.Take(20).Select(r => new DailyReportSummary(
            r.Id,
            r.Date,
            r.Status.ToString(),
            r.WorkHours.Sum(wh => wh.Hours),
            r.WorkEntries.Count,
            r.MaterialUsages.Count,
            r.Notes)).ToList();

        var workTypeStats = CalculateWorkTypeStats(reports);
        var materialStats = CalculateMaterialStats(reports);
        var workerStats = CalculateWorkerStats(reports);
        var workerRankings = CalculateWorkerRankings(workerStats);

        return new CrewStatsResponse(
            request.CrewId,
            crewName,
            projectName,
            dateFrom,
            dateTo,
            reports.Count,
            reports.Sum(r => r.WorkHours.Sum(wh => wh.Hours)),
            reports.Sum(r => r.MaterialUsages.Sum(mu => mu.Quantity)),
            recentReports,
            workTypeStats,
            materialStats,
            workerStats,
            workerRankings);
    }

    private static List<WorkTypeStats> CalculateWorkTypeStats(List<DailyReport> reports)
    {
        var workEntries = reports.SelectMany(r => r.WorkEntries).ToList();
        var workHoursByReport = reports.ToDictionary(r => r.Id, r => r.WorkHours.Sum(wh => wh.Hours));

        return workEntries
            .GroupBy(we => we.WorkTypeId)
            .Select(g =>
            {
                var workType = g.First().WorkType;
                var totalQuantity = g.Sum(we => we.Quantity);
                var reportIds = g.Select(we => we.DailyReportId).Distinct().ToList();
                var totalHours = reportIds.Sum(id => workHoursByReport.GetValueOrDefault(id, 0));
                var avgHoursPerUnit = totalQuantity > 0 ? totalHours / totalQuantity : 0;

                return new WorkTypeStats(
                    g.Key,
                    workType?.Name ?? "Unknown",
                    workType?.Code ?? "",
                    totalQuantity,
                    totalHours,
                    avgHoursPerUnit,
                    reportIds.Count);
            })
            .OrderByDescending(s => s.TotalHours)
            .ToList();
    }

    private static List<MaterialStats> CalculateMaterialStats(List<DailyReport> reports)
    {
        var materialUsages = reports.SelectMany(r => r.MaterialUsages).ToList();

        return materialUsages
            .GroupBy(mu => mu.MaterialId)
            .Select(g =>
            {
                var material = g.First().Material;
                var totalQuantity = g.Sum(mu => mu.Quantity);
                var reportCount = g.Select(mu => mu.DailyReportId).Distinct().Count();

                return new MaterialStats(
                    g.Key,
                    material?.Name ?? "Unknown",
                    material?.Unit ?? "",
                    totalQuantity,
                    reportCount > 0 ? totalQuantity / reportCount : 0,
                    reportCount);
            })
            .OrderByDescending(s => s.TotalQuantity)
            .ToList();
    }

    private static List<WorkerStats> CalculateWorkerStats(List<DailyReport> reports)
    {
        var allWorkHours = reports.SelectMany(r => r.WorkHours).ToList();
        var workEntries = reports.SelectMany(r => r.WorkEntries).ToList();

        var workers = new Dictionary<(Guid Id, bool IsSubco), (string Name, decimal TotalHours, HashSet<Guid> ReportIds)>();

        foreach (var wh in allWorkHours)
        {
            Guid workerId;
            string workerName;
            bool isSubco;

            if (wh.SubcontractorWorkerId.HasValue && wh.SubcontractorWorker != null)
            {
                workerId = wh.SubcontractorWorkerId.Value;
                workerName = $"{wh.SubcontractorWorker.FirstName} {wh.SubcontractorWorker.LastName}";
                isSubco = true;
            }
            else if (wh.WorkerId.HasValue && wh.Worker != null)
            {
                workerId = wh.WorkerId.Value;
                workerName = $"{wh.Worker.FirstName} {wh.Worker.LastName}";
                isSubco = false;
            }
            else
            {
                continue;
            }

            var key = (workerId, isSubco);
            if (!workers.ContainsKey(key))
            {
                workers[key] = (workerName, 0, new HashSet<Guid>());
            }

            var current = workers[key];
            current.TotalHours += wh.Hours;
            current.ReportIds.Add(wh.DailyReportId);
            workers[key] = current;
        }

        return workers.Select(kvp =>
        {
            var (id, isSubco) = kvp.Key;
            var (name, totalHours, reportIds) = kvp.Value;
            var avgHours = reportIds.Count > 0 ? totalHours / reportIds.Count : 0;

            var workerWorkTypeBreakdown = CalculateWorkerWorkTypeBreakdown(
                reports, id, isSubco, totalHours);

            return new WorkerStats(
                id,
                name,
                isSubco,
                totalHours,
                reportIds.Count,
                avgHours,
                workerWorkTypeBreakdown);
        })
        .OrderByDescending(ws => ws.TotalHours)
        .ToList();
    }

    private static List<WorkerWorkTypeBreakdown> CalculateWorkerWorkTypeBreakdown(
        List<DailyReport> reports,
        Guid workerId,
        bool isSubco,
        decimal totalHours)
    {
        var workerReportIds = reports
            .Where(r => r.WorkHours.Any(wh =>
                isSubco
                    ? wh.SubcontractorWorkerId == workerId
                    : wh.WorkerId == workerId))
            .Select(r => r.Id)
            .ToHashSet();

        var workTypes = reports
            .Where(r => workerReportIds.Contains(r.Id))
            .SelectMany(r => r.WorkEntries)
            .GroupBy(we => we.WorkType?.Name ?? "Unknown")
            .Select(g =>
            {
                var hoursForType = reports
                    .Where(r => r.WorkEntries.Any(we => we.WorkType?.Name == g.Key))
                    .SelectMany(r => r.WorkHours)
                    .Where(wh => isSubco
                        ? wh.SubcontractorWorkerId == workerId
                        : wh.WorkerId == workerId)
                    .Sum(wh => wh.Hours);

                return new WorkerWorkTypeBreakdown(
                    g.Key,
                    hoursForType,
                    totalHours > 0 ? hoursForType / totalHours * 100 : 0);
            })
            .OrderByDescending(b => b.Hours)
            .Take(5)
            .ToList();

        return workTypes;
    }

    private static List<WorkerRanking> CalculateWorkerRankings(List<WorkerStats> workerStats)
    {
        if (workerStats.Count == 0)
            return [];

        var avgHoursAll = workerStats.Average(ws => ws.TotalHours);

        return workerStats
            .Select((ws, index) =>
            {
                var deviation = avgHoursAll > 0
                    ? (ws.TotalHours - avgHoursAll) / avgHoursAll * 100
                    : 0;

                var category = deviation switch
                {
                    > 20 => "top",
                    < -20 => "below",
                    _ => "average"
                };

                return new WorkerRanking(
                    ws.WorkerId,
                    ws.WorkerName,
                    ws.IsSubcontractorWorker,
                    ws.TotalHours,
                    ws.AverageHoursPerReport,
                    deviation,
                    index + 1,
                    category);
            })
            .ToList();
    }
}

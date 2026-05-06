namespace SRG.Application.Analytics;

public record CrewStatsRequest(
    Guid CrewId,
    DateOnly? DateFrom,
    DateOnly? DateTo);

public record CrewStatsResponse(
    Guid CrewId,
    string CrewName,
    string ProjectName,
    DateOnly DateFrom,
    DateOnly DateTo,
    int TotalReports,
    decimal TotalHours,
    decimal TotalMaterialsUsed,
    List<DailyReportSummary> RecentReports,
    List<WorkTypeStats> WorkTypeStats,
    List<MaterialStats> MaterialStats,
    List<WorkerStats> WorkerStats,
    List<WorkerRanking> WorkerRankings);

public record DailyReportSummary(
    Guid Id,
    DateOnly Date,
    string Status,
    decimal TotalHours,
    int WorkEntryCount,
    int MaterialUsageCount,
    string? Notes);

public record WorkTypeStats(
    Guid WorkTypeId,
    string WorkTypeName,
    string Code,
    decimal TotalQuantity,
    decimal TotalHours,
    decimal AverageHoursPerUnit,
    int ReportCount);

public record MaterialStats(
    Guid MaterialId,
    string MaterialName,
    string Unit,
    decimal TotalQuantity,
    decimal AveragePerReport,
    int ReportCount);

public record WorkerStats(
    Guid WorkerId,
    string WorkerName,
    bool IsSubcontractorWorker,
    decimal TotalHours,
    int ReportCount,
    decimal AverageHoursPerReport,
    List<WorkerWorkTypeBreakdown> WorkTypeBreakdown);

public record WorkerWorkTypeBreakdown(
    string WorkTypeName,
    decimal Hours,
    decimal Percentage);

public record WorkerRanking(
    Guid WorkerId,
    string WorkerName,
    bool IsSubcontractorWorker,
    decimal TotalHours,
    decimal AverageHoursPerReport,
    /// <summary>
    /// Odchylenie od średniej w procentach. Dodatnie = więcej niż średnia, ujemne = mniej.
    /// </summary>
    decimal DeviationFromAverage,
    /// <summary>
    /// Pozycja w rankingu (1 = najlepszy)
    /// </summary>
    int Rank,
    /// <summary>
    /// Kategoria: "top", "average", "below"
    /// </summary>
    string Category);

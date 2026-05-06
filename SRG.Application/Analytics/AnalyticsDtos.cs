namespace SRG.Application.Analytics;

public record PMAnalyticsResponse(
    DailyReportStatsResponse DailyReportStats,
    List<WorkProgressByProjectResponse> WorkProgressByProject,
    List<TopCrewResponse> TopCrews,
    List<DailyReportOverTimeResponse> DailyReportOverTime);

public record DailyReportStatsResponse(int Total, int Approved, int Rejected, int Pending);

public record WorkProgressByProjectResponse(Guid ProjectId, string ProjectName, int TotalWorkEntries, decimal TotalQuantity);

public record TopCrewResponse(Guid CrewId, string CrewName, int DailyReportCount);

public record DailyReportOverTimeResponse(DateOnly Date, int Count);

public record LogisticsAnalyticsResponse(
    int TotalMaterials,
    int LowStockCount,
    List<MaterialUsageSummaryResponse> MaterialUsage,
    List<LowStockResponse> LowStock,
    WarehouseFlowResponse WarehouseFlow);

public record MaterialUsageSummaryResponse(Guid MaterialId, string MaterialName, decimal TotalUsed);

public record LowStockResponse(Guid MaterialId, string MaterialName, string Unit, decimal Quantity);

public record WarehouseFlowResponse(decimal Issued, decimal Returned);

public record ForemanAnalyticsResponse(
    int TotalDailyReport,
    decimal TotalHours,
    int TotalWorkEntries,
    List<HoursOverTimeResponse> HoursOverTime);

public record HoursOverTimeResponse(DateOnly Date, decimal Hours);

public record CrewAnalyticsResponse(
    Guid CrewId,
    string CrewName,
    CrewWorkStatsResponse WorkStats,
    List<CrewMaterialUsageResponse> MaterialUsage,
    List<WorkerStatsResponse> WorkerStats);

public record CrewMaterialUsageResponse(
    Guid MaterialId,
    string MaterialName,
    string Unit,
    decimal TotalUsed,
    decimal AverageDailyUsage);

public record CrewWorkStatsResponse(
    int TotalWorkEntries,
    decimal TotalHours,
    decimal AverageHoursPerDay);

public record WorkerStatsResponse(
    Guid WorkerId,
    string WorkerName,
    decimal TotalHours,
    decimal AverageHoursPerDay,
    int DaysWorked);

public record MaterialStatsResponse(
    Guid MaterialId,
    string MaterialName,
    string Unit,
    string CategoryName,
    decimal CurrentStock,
    decimal TotalReceived,
    decimal TotalIssued,
    decimal TotalUsed,
    List<MaterialUsageByCrewResponse> UsageByCrew,
    List<MaterialUsageOverTimeResponse> UsageOverTime,
    List<MaterialDeliveryResponse> Deliveries);

public record MaterialUsageByCrewResponse(
    Guid CrewId,
    string CrewName,
    decimal TotalUsed);

public record MaterialUsageOverTimeResponse(
    DateOnly Date,
    decimal Quantity);

public record MaterialDeliveryResponse(
    Guid GrvId,
    string GrvNumber,
    DateOnly DeliveryDate,
    decimal Quantity,
    string? SupplierName);

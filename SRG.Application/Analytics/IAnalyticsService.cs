namespace SRG.Application.Analytics;

public interface IAnalyticsService
{
    Task<PMAnalyticsResponse> GetPMAnalyticsAsync(CancellationToken cancellationToken = default);
    Task<LogisticsAnalyticsResponse> GetLogisticsAnalyticsAsync(decimal lowStockThreshold = 10, CancellationToken cancellationToken = default);
    Task<ForemanAnalyticsResponse> GetForemanAnalyticsAsync(Guid foremanId, CancellationToken cancellationToken = default);
    Task<CrewAnalyticsResponse> GetCrewAnalyticsAsync(Guid crewId, DateOnly? dateFrom = null, DateOnly? dateTo = null, CancellationToken cancellationToken = default);
    Task<List<CrewMaterialUsageResponse>> GetCrewMaterialUsageAsync(Guid crewId, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default);
    Task<List<CrewMaterialUsageResponse>> GetCrewMaterialAverageAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task<CrewWorkStatsResponse> GetCrewWorkStatsAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task<List<WorkerStatsResponse>> GetWorkerStatsAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task<MaterialStatsResponse> GetMaterialStatsAsync(Guid materialId, CancellationToken cancellationToken = default);
}

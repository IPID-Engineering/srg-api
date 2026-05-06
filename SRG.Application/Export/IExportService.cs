namespace SRG.Application.Export;

public interface IExportService
{
    Task<ExportFileResponse> ExportDailyReportToExcelAsync(Guid dailyReportId, CancellationToken cancellationToken = default);
    Task<ExportFileResponse> ExportDailyReportToPdfAsync(Guid dailyReportId, CancellationToken cancellationToken = default);
    Task<ExportFileResponse> ExportMaterialUsageToExcelAsync(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default);
    Task<ExportFileResponse> ExportWarehouseStockToExcelAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Application.Export;

namespace SRG.Api.Controllers;

[ApiController]
[Route("export")]
[Authorize]
public class ExportController(IExportService exportService) : ControllerBase
{
    [HttpGet("daily-reports/{id:guid}/excel")]
    [Authorize(Roles = "Foreman,SubcontractorForeman,PM,SPM")]
    public async Task<IActionResult> ExportDailyReportExcel(Guid id, CancellationToken cancellationToken)
    {
        return await Download(() => exportService.ExportDailyReportToExcelAsync(id, cancellationToken));
    }

    [HttpGet("daily-reports/{id:guid}/pdf")]
    [Authorize(Roles = "Foreman,SubcontractorForeman,PM,SPM")]
    public async Task<IActionResult> ExportDailyReportPdf(Guid id, CancellationToken cancellationToken)
    {
        return await Download(() => exportService.ExportDailyReportToPdfAsync(id, cancellationToken));
    }

    [HttpGet("materials")]
    [Authorize(Roles = "Logistician,PM,SPM")]
    public async Task<IActionResult> ExportMaterialUsage(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        return await Download(() => exportService.ExportMaterialUsageToExcelAsync(from, to, cancellationToken));
    }

    [HttpGet("warehouses/{id:guid}")]
    [Authorize(Roles = "Logistician")]
    public async Task<IActionResult> ExportWarehouseStock(Guid id, CancellationToken cancellationToken)
    {
        return await Download(() => exportService.ExportWarehouseStockToExcelAsync(id, cancellationToken));
    }

    private async Task<IActionResult> Download(Func<Task<ExportFileResponse>> createFile)
    {
        try
        {
            var file = await createFile();
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}

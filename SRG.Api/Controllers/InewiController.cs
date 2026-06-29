using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SRG.Api.Extensions;
using SRG.Application.Inewi;
using SRG.Application.Persistence;
using SRG.Infrastructure.Inewi;

namespace SRG.Api.Controllers;

[ApiController]
[Route("inewi")]
[Authorize(Roles = "Subcontractor")]
public class InewiController(
    IInewiService inewiService,
    IInewiIntegrationService integrationService,
    IConstructionRepository constructionRepository,
    ILogger<InewiController> logger) : ControllerBase
{
    /// <summary>
    /// Get all INEWI records for the current subcontractor.
    /// </summary>
    [HttpGet("records")]
    public async Task<ActionResult<List<InewiRecordResponse>>> GetRecords(CancellationToken cancellationToken)
    {
        try
        {
            var subcontractorId = User.GetSubcontractorIdRequired();
            return Ok(await inewiService.GetBySubcontractorAsync(subcontractorId, cancellationToken));
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się pobrać rekordów INEWI." });
        }
    }
    
    /// <summary>
    /// Get enriched INEWI records with crew/company info and filter options.
    /// </summary>
    [HttpGet("records/enriched")]
    public async Task<ActionResult<InewiRecordsPageResponse>> GetEnrichedRecords(CancellationToken cancellationToken)
    {
        try
        {
            var subcontractorId = User.GetSubcontractorIdRequired();
            return Ok(await inewiService.GetEnrichedRecordsAsync(subcontractorId, cancellationToken));
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się pobrać rekordów INEWI." });
        }
    }

    /// <summary>
    /// Get INEWI records for a date range.
    /// </summary>
    [HttpGet("records/range")]
    public async Task<ActionResult<List<InewiRecordResponse>>> GetByDateRange(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        try
        {
            if (from > to)
            {
                return BadRequest(new { message = "Data początkowa nie może być późniejsza niż data końcowa." });
            }
            
            var subcontractorId = User.GetSubcontractorIdRequired();
            return Ok(await inewiService.GetByDateRangeAsync(subcontractorId, from, to, cancellationToken));
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się pobrać rekordów INEWI." });
        }
    }

    /// <summary>
    /// Import INEWI records manually.
    /// </summary>
    [HttpPost("records/import")]
    public async Task<ActionResult<ImportInewiResult>> Import(
        [FromBody] ImportInewiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Records == null || request.Records.Count == 0)
            {
                return BadRequest(new { message = "Brak rekordów do importu." });
            }
            
            if (request.Records.Count > 10000)
            {
                return BadRequest(new { message = "Maksymalnie 10000 rekordów w jednym imporcie." });
            }
            
            var subcontractorId = User.GetSubcontractorIdRequired();
            var userId = User.GetUserId();
            var result = await inewiService.ImportAsync(subcontractorId, userId, request.Records, request.SourceFileName, cancellationToken);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się zaimportować rekordów INEWI." });
        }
    }

    /// <summary>
    /// Get integration status for the current subcontractor.
    /// </summary>
    [HttpGet("integration")]
    public async Task<ActionResult<InewiIntegrationStatusResponse>> GetIntegrationStatus(CancellationToken cancellationToken)
    {
        try
        {
            var subcontractorId = User.GetSubcontractorIdRequired();
            return Ok(await integrationService.GetIntegrationStatusAsync(subcontractorId, cancellationToken));
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się pobrać statusu integracji." });
        }
    }

    /// <summary>
    /// Configure integration with inewi API.
    /// </summary>
    [HttpPost("integration")]
    public async Task<ActionResult<InewiIntegrationStatusResponse>> ConfigureIntegration(
        [FromBody] ConfigureInewiIntegrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var subcontractorId = User.GetSubcontractorIdRequired();
            var userId = User.GetUserId();
            return Ok(await integrationService.ConfigureIntegrationAsync(subcontractorId, userId, request, cancellationToken));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InewiApiException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to configure inewi integration");
            return StatusCode(500, new { message = "Nie udało się skonfigurować integracji." });
        }
    }

    /// <summary>
    /// Sync data from inewi API.
    /// </summary>
    [HttpPost("integration/sync")]
    public async Task<ActionResult<InewiSyncResult>> SyncData(
        [FromBody] InewiSyncRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.From > request.To)
            {
                return BadRequest(new { message = "Data początkowa nie może być późniejsza niż data końcowa." });
            }
            
            var subcontractorId = User.GetSubcontractorIdRequired();
            var userId = User.GetUserId();
            return Ok(await integrationService.SyncDataAsync(subcontractorId, userId, request, cancellationToken));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InewiApiException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się zsynchronizować danych." });
        }
    }
    
    /// <summary>
    /// Debug endpoint to diagnose sync issues.
    /// </summary>
    [HttpPost("integration/sync-debug")]
    public async Task<ActionResult> SyncDebug(
        [FromBody] InewiSyncRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var subcontractorId = User.GetSubcontractorIdRequired();
            var result = await integrationService.DebugSyncAsync(subcontractorId, request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Disable integration with inewi API.
    /// </summary>
    [HttpDelete("integration")]
    public async Task<ActionResult> DisableIntegration(CancellationToken cancellationToken)
    {
        try
        {
            var subcontractorId = User.GetSubcontractorIdRequired();
            await integrationService.DisableIntegrationAsync(subcontractorId, cancellationToken);
            return Ok(new { message = "Integracja została wyłączona." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się wyłączyć integracji." });
        }
    }

    /// <summary>
    /// Get list of employees from inewi organization for mapping.
    /// </summary>
    [HttpGet("integration/employees")]
    public async Task<ActionResult<InewiEmployeesListResponse>> GetInewiEmployees(CancellationToken cancellationToken)
    {
        try
        {
            var subcontractorId = User.GetSubcontractorIdRequired();
            return Ok(await integrationService.GetInewiEmployeesAsync(subcontractorId, cancellationToken));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InewiApiException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się pobrać listy pracowników z inewi." });
        }
    }

    /// <summary>
    /// Sync workers to inewi - create employees in inewi with job positions.
    /// </summary>
    [HttpPost("integration/sync-workers")]
    public async Task<ActionResult<InewiWorkersSyncResult>> SyncWorkersToInewi(CancellationToken cancellationToken)
    {
        try
        {
            var subcontractorId = User.GetSubcontractorIdRequired();
            var result = await integrationService.SyncWorkersToInewiAsync(subcontractorId, cancellationToken);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InewiApiException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync workers to inewi");
            return StatusCode(500, new { message = "Nie udało się zsynchronizować pracowników." });
        }
    }

    /// <summary>
    /// Map a worker to an inewi employee.
    /// </summary>
    [HttpPut("workers/{workerId:guid}/mapping")]
    public async Task<ActionResult> MapWorkerToInewiEmployee(
        Guid workerId,
        [FromBody] MapWorkerToInewiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var subcontractorId = User.GetSubcontractorIdRequired();
            
            var worker = await constructionRepository.GetSubcontractorWorkerByIdAsync(workerId, cancellationToken);
            if (worker == null)
            {
                return NotFound(new { message = "Nie znaleziono pracownika." });
            }
            
            // Verify the worker belongs to the current subcontractor
            if (worker.SubcontractorId != subcontractorId)
            {
                return Forbid();
            }
            
            await integrationService.MapWorkerToInewiEmployeeAsync(workerId, request.InewiEmployeeId, cancellationToken);
            return Ok(new { message = "Mapowanie zostało zapisane." });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się zapisać mapowania." });
        }
    }

    /// <summary>
    /// Generate QR codes for employees (returns PDF URL for printing).
    /// </summary>
    [HttpPost("integration/print-qr")]
    public async Task<ActionResult<InewiPrintQrResult>> PrintQrCodes(
        [FromBody] InewiPrintQrRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var subcontractorId = User.GetSubcontractorIdRequired();
            return Ok(await integrationService.PrintQrCodesAsync(subcontractorId, request, cancellationToken));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InewiApiException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate QR codes");
            return StatusCode(500, new { message = "Nie udało się wygenerować kodów QR." });
        }
    }

    /// <summary>
    /// Get detailed time events report for a specific date (clock-in/out, breaks).
    /// </summary>
    [HttpGet("integration/detailed-report")]
    public async Task<ActionResult<InewiWorkersDetailedReportResponse>> GetDetailedReport(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        try
        {
            var subcontractorId = User.GetSubcontractorIdRequired();
            return Ok(await integrationService.GetDetailedReportAsync(subcontractorId, date, cancellationToken));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InewiApiException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get detailed report from inewi");
            return StatusCode(500, new { message = "Nie udało się pobrać szczegółowego raportu z inewi." });
        }
    }
}

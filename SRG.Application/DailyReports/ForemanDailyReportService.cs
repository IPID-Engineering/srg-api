using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.DailyReports;

public interface IForemanDailyReportService
{
    Task<ForemanDkpResponse> GetOrCreateForDateAsync(DateOnly date, Guid crewId, Guid userId, CancellationToken cancellationToken);
    Task<List<ForemanDkpCalendarItem>> GetCalendarWithAutoCreateAsync(Guid crewId, Guid userId, CancellationToken cancellationToken);
    Task<ForemanDkpResponse> AddHoursAsync(Guid reportId, AddForemanHoursRequest request, Guid crewId, CancellationToken cancellationToken);
    Task<ForemanDkpResponse> AddWorkAsync(Guid reportId, AddForemanWorkRequest request, Guid crewId, CancellationToken cancellationToken);
    Task<ForemanDkpResponse> AddMaterialAsync(Guid reportId, AddForemanMaterialRequest request, Guid crewId, CancellationToken cancellationToken);
    Task<ForemanDkpResponse> UpdateNotesAsync(Guid reportId, string? notes, Guid crewId, CancellationToken cancellationToken);
    Task<ForemanDkpResponse> UpdateWorkOrderAsync(Guid reportId, Guid? workOrderId, Guid crewId, CancellationToken cancellationToken);
    Task<ForemanDkpResponse> AddWorkOrderAsync(Guid reportId, Guid workOrderId, Guid crewId, CancellationToken cancellationToken);
    Task<ForemanDkpResponse> RemoveWorkOrderAsync(Guid reportId, Guid workOrderId, Guid crewId, CancellationToken cancellationToken);
    Task<ForemanDkpResponse> SubmitAsync(Guid reportId, Guid crewId, CancellationToken cancellationToken);
}

public class ForemanDailyReportService(
    IDailyReportRepository dailyReportRepository,
    IConstructionRepository constructionRepository) : IForemanDailyReportService
{
    public async Task<ForemanDkpResponse> GetOrCreateForDateAsync(DateOnly date, Guid crewId, Guid userId, CancellationToken cancellationToken)
    {
        var report = await dailyReportRepository.GetBySubcontractorCrewAndDateAsync(crewId, date, cancellationToken);

        if (report == null)
        {
            report = await CreateReportForDateAsync(crewId, userId, date, cancellationToken);
        }

        return MapToResponse(report);
    }

    public async Task<List<ForemanDkpCalendarItem>> GetCalendarWithAutoCreateAsync(Guid crewId, Guid userId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var startDate = today.AddDays(-7);
        var endDate = today.AddDays(7);

        var existingReports = await dailyReportRepository.GetByCrewDateRangeAsync(crewId, startDate, endDate, cancellationToken);

        var result = new List<ForemanDkpCalendarItem>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var report = existingReports.FirstOrDefault(r => r.Date == date);
            
            if (report == null)
            {
                report = await CreateReportForDateAsync(crewId, userId, date, cancellationToken);
            }

            result.Add(new ForemanDkpCalendarItem
            {
                Id = report.Id,
                Date = report.Date,
                Status = report.Status.ToString(),
                TotalHours = report.WorkHours.Sum(wh => wh.Hours),
                HasWork = report.WorkHours.Count != 0
            });
        }

        return result.OrderBy(x => x.Date).ToList();
    }

    public async Task<ForemanDkpResponse> AddHoursAsync(Guid reportId, AddForemanHoursRequest request, Guid crewId, CancellationToken cancellationToken)
    {
        var report = await GetReportWithAccessCheckAsync(reportId, crewId, cancellationToken);

        if (report.Status != DailyReportStatus.Draft && report.Status != DailyReportStatus.Rejected)
            throw new ValidationException("Nie można edytować karty która nie jest szkicem lub nie wymaga poprawek.");

        var worker = await constructionRepository.GetSubcontractorWorkerByIdAsync(request.WorkerId, cancellationToken)
            ?? throw new KeyNotFoundException("Pracownik nie został znaleziony.");

        var existingHours = report.WorkHours.FirstOrDefault(wh => wh.SubcontractorWorkerId == request.WorkerId);
        if (existingHours != null)
        {
            existingHours.Hours = request.Hours;
        }
        else
        {
            var newWorkHour = new WorkHour
            {
                Id = Guid.NewGuid(),
                DailyReportId = reportId,
                SubcontractorWorkerId = request.WorkerId,
                Hours = request.Hours
            };
            await dailyReportRepository.AddWorkHoursAsync(newWorkHour, cancellationToken);
        }

        await dailyReportRepository.SaveChangesAsync(cancellationToken);

        return await GetReportResponseAsync(reportId, cancellationToken);
    }

    public async Task<ForemanDkpResponse> AddWorkAsync(Guid reportId, AddForemanWorkRequest request, Guid crewId, CancellationToken cancellationToken)
    {
        var report = await GetReportWithAccessCheckAsync(reportId, crewId, cancellationToken);

        if (report.Status != DailyReportStatus.Draft && report.Status != DailyReportStatus.Rejected)
            throw new ValidationException("Nie można edytować karty która nie jest szkicem lub nie wymaga poprawek.");

        var existingEntry = request.OrderedWorkId.HasValue
            ? report.WorkEntries.FirstOrDefault(we => we.OrderedWorkId == request.OrderedWorkId)
            : report.WorkEntries.FirstOrDefault(we => we.WorkTypeId == request.WorkTypeId && we.OrderedWorkId == null);
        
        if (existingEntry != null)
        {
            existingEntry.Quantity = request.Quantity;
            existingEntry.Description = request.Description;
        }
        else
        {
            var workEntry = new WorkEntry
            {
                Id = Guid.NewGuid(),
                DailyReportId = reportId,
                WorkTypeId = request.WorkTypeId,
                OrderedWorkId = request.OrderedWorkId,
                Quantity = request.Quantity,
                Description = request.Description
            };
            await dailyReportRepository.AddWorkEntryAsync(workEntry, cancellationToken);
        }
        await dailyReportRepository.SaveChangesAsync(cancellationToken);

        return await GetReportResponseAsync(reportId, cancellationToken);
    }

    public async Task<ForemanDkpResponse> AddMaterialAsync(Guid reportId, AddForemanMaterialRequest request, Guid crewId, CancellationToken cancellationToken)
    {
        var report = await GetReportWithAccessCheckAsync(reportId, crewId, cancellationToken);

        if (report.Status != DailyReportStatus.Draft && report.Status != DailyReportStatus.Rejected)
            throw new ValidationException("Nie można edytować karty która nie jest szkicem lub nie wymaga poprawek.");

        var existingUsage = report.MaterialUsages.FirstOrDefault(mu => mu.MaterialId == request.MaterialId);
        if (existingUsage != null)
        {
            existingUsage.Quantity = request.Quantity;
        }
        else
        {
            var materialUsage = new MaterialUsage
            {
                Id = Guid.NewGuid(),
                DailyReportId = reportId,
                MaterialId = request.MaterialId,
                Quantity = request.Quantity
            };
            await dailyReportRepository.AddMaterialAsync(materialUsage, cancellationToken);
        }
        await dailyReportRepository.SaveChangesAsync(cancellationToken);

        return await GetReportResponseAsync(reportId, cancellationToken);
    }

    public async Task<ForemanDkpResponse> UpdateNotesAsync(Guid reportId, string? notes, Guid crewId, CancellationToken cancellationToken)
    {
        var report = await GetReportWithAccessCheckAsync(reportId, crewId, cancellationToken);

        if (report.Status != DailyReportStatus.Draft && report.Status != DailyReportStatus.Rejected)
            throw new ValidationException("Nie można edytować karty która nie jest szkicem lub nie wymaga poprawek.");

        report.Notes = notes;
        await dailyReportRepository.SaveChangesAsync(cancellationToken);

        return await GetReportResponseAsync(reportId, cancellationToken);
    }

    public async Task<ForemanDkpResponse> UpdateWorkOrderAsync(Guid reportId, Guid? workOrderId, Guid crewId, CancellationToken cancellationToken)
    {
        var report = await GetReportWithAccessCheckAsync(reportId, crewId, cancellationToken);

        if (report.Status != DailyReportStatus.Draft && report.Status != DailyReportStatus.Rejected)
            throw new ValidationException("Nie można edytować karty która nie jest szkicem lub nie wymaga poprawek.");

        report.WorkOrderId = workOrderId;
        await dailyReportRepository.SaveChangesAsync(cancellationToken);

        return await GetReportResponseAsync(reportId, cancellationToken);
    }

    public async Task<ForemanDkpResponse> AddWorkOrderAsync(Guid reportId, Guid workOrderId, Guid crewId, CancellationToken cancellationToken)
    {
        var report = await GetReportWithAccessCheckAsync(reportId, crewId, cancellationToken);

        if (report.Status != DailyReportStatus.Draft && report.Status != DailyReportStatus.Rejected)
            throw new ValidationException("Nie można edytować karty która nie jest szkicem lub nie wymaga poprawek.");

        if (report.DailyReportWorkOrders.Any(drwo => drwo.WorkOrderId == workOrderId))
            throw new ValidationException("To zlecenie jest już dodane do tej karty.");

        var dailyReportWorkOrder = new DailyReportWorkOrder
        {
            Id = Guid.NewGuid(),
            DailyReportId = reportId,
            WorkOrderId = workOrderId,
            AddedAt = DateTime.UtcNow
        };

        report.DailyReportWorkOrders.Add(dailyReportWorkOrder);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);

        return await GetReportResponseAsync(reportId, cancellationToken);
    }

    public async Task<ForemanDkpResponse> RemoveWorkOrderAsync(Guid reportId, Guid workOrderId, Guid crewId, CancellationToken cancellationToken)
    {
        var report = await GetReportWithAccessCheckAsync(reportId, crewId, cancellationToken);

        if (report.Status != DailyReportStatus.Draft && report.Status != DailyReportStatus.Rejected)
            throw new ValidationException("Nie można edytować karty która nie jest szkicem lub nie wymaga poprawek.");

        var dailyReportWorkOrder = report.DailyReportWorkOrders.FirstOrDefault(drwo => drwo.WorkOrderId == workOrderId)
            ?? throw new KeyNotFoundException("To zlecenie nie jest przypisane do tej karty.");

        report.DailyReportWorkOrders.Remove(dailyReportWorkOrder);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);

        return await GetReportResponseAsync(reportId, cancellationToken);
    }

    public async Task<ForemanDkpResponse> SubmitAsync(Guid reportId, Guid crewId, CancellationToken cancellationToken)
    {
        var report = await GetReportWithAccessCheckAsync(reportId, crewId, cancellationToken);

        if (report.Status != DailyReportStatus.Draft && report.Status != DailyReportStatus.Rejected)
            throw new ValidationException("Karta nie jest szkicem lub nie wymaga poprawek.");

        if (report.WorkHours.Count == 0)
            throw new ValidationException("Karta musi zawierać przynajmniej jednego pracownika z godzinami.");

        report.Status = DailyReportStatus.Submitted;
        await dailyReportRepository.SaveChangesAsync(cancellationToken);

        return await GetReportResponseAsync(reportId, cancellationToken);
    }

    private async Task<DailyReport> GetReportWithAccessCheckAsync(Guid reportId, Guid crewId, CancellationToken cancellationToken)
    {
        var report = await dailyReportRepository.GetByIdWithWorkersAsync(reportId, cancellationToken);
        if (report == null || report.SubcontractorCrewId != crewId)
            throw new KeyNotFoundException("Karta pracy nie została znaleziona.");

        return report;
    }

    private async Task<DailyReport> CreateReportForDateAsync(Guid crewId, Guid userId, DateOnly date, CancellationToken cancellationToken)
    {
        var crew = await constructionRepository.GetSubcontractorCrewByIdAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Brygada nie została znaleziona.");

        // Dla brygad podwykonawców nie ustawiamy CrewId, ProjectId, SectionId, CreatedById
        // Raport jest powiązany tylko przez SubcontractorCrewId
        // CreatedById jest null bo brygadzista to SubcontractorWorker, nie User
        var report = new DailyReport
        {
            Id = Guid.NewGuid(),
            Date = date,
            Status = DailyReportStatus.Draft,
            SubcontractorCrewId = crewId,
            CrewId = null,
            ProjectId = null,
            SectionId = null,
            CreatedById = null,
            CreatedAt = DateTime.UtcNow,
            Notes = null
        };

        await dailyReportRepository.AddAsync(report, cancellationToken);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);

        return report;
    }

    private async Task<ForemanDkpResponse> GetReportResponseAsync(Guid reportId, CancellationToken cancellationToken)
    {
        var report = await dailyReportRepository.GetByIdWithWorkersAsync(reportId, cancellationToken)
            ?? throw new KeyNotFoundException("Karta pracy nie została znaleziona.");

        return MapToResponse(report);
    }

    private static ForemanDkpResponse MapToResponse(DailyReport report)
    {
        return new ForemanDkpResponse
        {
            Id = report.Id,
            Date = report.Date,
            Status = report.Status.ToString(),
            Notes = report.Notes,
            WorkOrderId = report.WorkOrderId,
            WorkOrderNumber = report.WorkOrder?.Number,
            TotalHours = report.WorkHours.Sum(wh => wh.Hours),
            HasUnresolvedComments = report.Comments.Any(c => !c.IsResolved && c.ParentCommentId == null),
            WorkHours = report.WorkHours.Select(wh => new ForemanWorkHoursItem
            {
                Id = wh.Id,
                WorkerId = wh.SubcontractorWorkerId ?? wh.WorkerId ?? Guid.Empty,
                WorkerName = wh.SubcontractorWorker != null 
                    ? $"{wh.SubcontractorWorker.FirstName} {wh.SubcontractorWorker.LastName}" 
                    : wh.Worker != null
                        ? $"{wh.Worker.FirstName} {wh.Worker.LastName}"
                        : "Nieznany",
                Hours = wh.Hours
            }).ToList(),
            WorkEntries = report.WorkEntries.Select(we => new ForemanWorkEntryItem
            {
                Id = we.Id,
                WorkTypeId = we.WorkTypeId,
                OrderedWorkId = we.OrderedWorkId,
                WorkOrderId = we.OrderedWork?.WorkOrderId,
                WorkOrderNumber = we.OrderedWork?.WorkOrder?.Number,
                WorkTypeName = we.WorkType?.Name ?? "Nieznany",
                WorkTypeCode = we.WorkType?.Code,
                Quantity = we.Quantity,
                Unit = we.WorkType?.Unit ?? "szt",
                PlannedQuantity = we.OrderedWork?.PlannedQuantity,
                Description = we.Description
            }).ToList(),
            MaterialUsages = report.MaterialUsages.Select(mu => new ForemanMaterialUsageItem
            {
                Id = mu.Id,
                MaterialId = mu.MaterialId,
                MaterialName = mu.Material?.Name ?? "Nieznany",
                Quantity = mu.Quantity,
                Unit = mu.Material?.Unit ?? "szt"
            }).ToList(),
            Comments = report.Comments
                .Where(c => c.ParentCommentId == null)
                .Select(c => new ForemanCommentItem
                {
                    Id = c.Id,
                    Section = c.Section.ToString(),
                    RecordId = c.RecordId,
                    AuthorEmail = c.Author?.Email ?? "Nieznany",
                    AuthorRole = c.Author?.Role.ToString() ?? "Nieznany",
                    Content = c.Content,
                    IsResolved = c.IsResolved,
                    CreatedAt = c.CreatedAt
                }).ToList(),
            WorkOrders = report.DailyReportWorkOrders.Select(drwo => new ForemanWorkOrderItem
            {
                WorkOrderId = drwo.WorkOrderId,
                WorkOrderNumber = drwo.WorkOrder?.Number ?? "",
                Description = drwo.WorkOrder?.Description,
                OrderedWorks = drwo.WorkOrder?.OrderedWorks.Select(ow => new ForemanOrderedWorkItem
                {
                    Id = ow.Id,
                    WorkTypeId = ow.WorkTypeId,
                    WorkTypeName = ow.WorkType?.Name ?? "Nieznany",
                    WorkTypeCode = ow.WorkType?.Code,
                    Unit = ow.Unit,
                    PlannedQuantity = ow.PlannedQuantity,
                    Description = ow.Description
                }).ToList() ?? []
            }).ToList()
        };
    }
}

// DTOs
public class ForemanDkpResponse
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public required string Status { get; set; }
    public string? Notes { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public decimal TotalHours { get; set; }
    public bool HasUnresolvedComments { get; set; }
    public List<ForemanWorkHoursItem> WorkHours { get; set; } = [];
    public List<ForemanWorkEntryItem> WorkEntries { get; set; } = [];
    public List<ForemanMaterialUsageItem> MaterialUsages { get; set; } = [];
    public List<ForemanCommentItem> Comments { get; set; } = [];
    public List<ForemanWorkOrderItem> WorkOrders { get; set; } = [];
}

public class ForemanWorkOrderItem
{
    public Guid WorkOrderId { get; set; }
    public required string WorkOrderNumber { get; set; }
    public string? Description { get; set; }
    public List<ForemanOrderedWorkItem> OrderedWorks { get; set; } = [];
}

public class ForemanOrderedWorkItem
{
    public Guid Id { get; set; }
    public Guid WorkTypeId { get; set; }
    public required string WorkTypeName { get; set; }
    public string? WorkTypeCode { get; set; }
    public required string Unit { get; set; }
    public decimal PlannedQuantity { get; set; }
    public string? Description { get; set; }
}

public class ForemanCommentItem
{
    public Guid Id { get; set; }
    public required string Section { get; set; }
    public Guid? RecordId { get; set; }
    public required string AuthorEmail { get; set; }
    public required string AuthorRole { get; set; }
    public required string Content { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ForemanWorkHoursItem
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public required string WorkerName { get; set; }
    public decimal Hours { get; set; }
}

public class ForemanWorkEntryItem
{
    public Guid Id { get; set; }
    public Guid WorkTypeId { get; set; }
    public Guid? OrderedWorkId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public required string WorkTypeName { get; set; }
    public string? WorkTypeCode { get; set; }
    public decimal Quantity { get; set; }
    public required string Unit { get; set; }
    public decimal? PlannedQuantity { get; set; }
    public string? Description { get; set; }
}

public class ForemanMaterialUsageItem
{
    public Guid Id { get; set; }
    public Guid MaterialId { get; set; }
    public required string MaterialName { get; set; }
    public decimal Quantity { get; set; }
    public required string Unit { get; set; }
}

public class ForemanDkpCalendarItem
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public required string Status { get; set; }
    public decimal TotalHours { get; set; }
    public bool HasWork { get; set; }
}

public record AddForemanHoursRequest(Guid WorkerId, decimal Hours);
public record AddForemanWorkRequest(Guid WorkTypeId, decimal Quantity, string? Description, Guid? OrderedWorkId);
public record AddForemanMaterialRequest(Guid MaterialId, decimal Quantity);

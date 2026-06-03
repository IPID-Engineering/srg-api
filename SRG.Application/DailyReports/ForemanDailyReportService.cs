using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using SRG.Application.Common;
using SRG.Application.Persistence;
using SRG.Application.Warehouses;
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
    IConstructionRepository constructionRepository,
    IWarehouseRepository warehouseRepository,
    ICurrentUserContext currentUserContext) : IForemanDailyReportService
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
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        return await dailyReportRepository.GetCalendarItemsAsync(crewId, startOfMonth, endOfMonth, cancellationToken);
    }

    public async Task<ForemanDkpResponse> AddHoursAsync(Guid reportId, AddForemanHoursRequest request, Guid crewId, CancellationToken cancellationToken)
    {
        var report = await GetReportWithAccessCheckAsync(reportId, crewId, cancellationToken);

        if (report.Status != DailyReportStatus.Draft && report.Status != DailyReportStatus.Rejected)
            throw new ValidationException("Nie można edytować karty która nie jest szkicem lub nie wymaga poprawek.");

        var worker = await constructionRepository.GetSubcontractorWorkerByIdAsync(request.WorkerId, cancellationToken)
            ?? throw new KeyNotFoundException("Pracownik nie został znaleziony.");

        var hours = request.IsAbsent ? 0 : request.Hours;

        var existingHours = report.WorkHours.FirstOrDefault(wh => wh.SubcontractorWorkerId == request.WorkerId);
        if (existingHours != null)
        {
            var oldValues = new { existingHours.Hours, existingHours.IsAbsent };
            existingHours.Hours = hours;
            existingHours.IsAbsent = request.IsAbsent;
            var newValues = new { Hours = hours, IsAbsent = request.IsAbsent };
            
            await RecordChangeAsync(reportId, "WorkHour", existingHours.Id, "Updated", oldValues, newValues, cancellationToken);
        }
        else
        {
            var newWorkHour = new WorkHour
            {
                Id = Guid.NewGuid(),
                DailyReportId = reportId,
                SubcontractorWorkerId = request.WorkerId,
                Hours = hours,
                IsAbsent = request.IsAbsent
            };
            await dailyReportRepository.AddWorkHoursAsync(newWorkHour, cancellationToken);
            
            await RecordChangeAsync(reportId, "WorkHour", newWorkHour.Id, "Created", null, new { Hours = hours, IsAbsent = request.IsAbsent, newWorkHour.SubcontractorWorkerId }, cancellationToken);
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
            var oldValues = new { 
                existingEntry.Quantity, 
                existingEntry.Description, 
                existingEntry.WorkerCount, 
                existingEntry.HoursSpent 
            };
            
            existingEntry.Quantity = request.Quantity;
            existingEntry.Description = request.Description;
            existingEntry.WorkerCount = request.WorkerCount;
            existingEntry.HoursSpent = request.HoursSpent;
            
            var newValues = new { 
                Quantity = request.Quantity, 
                Description = request.Description, 
                WorkerCount = request.WorkerCount, 
                HoursSpent = request.HoursSpent 
            };
            
            await RecordChangeAsync(reportId, "WorkEntry", existingEntry.Id, "Updated", oldValues, newValues, cancellationToken);
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
                Description = request.Description,
                WorkerCount = request.WorkerCount,
                HoursSpent = request.HoursSpent,
                IsAddedByForeman = !request.OrderedWorkId.HasValue
            };
            await dailyReportRepository.AddWorkEntryAsync(workEntry, cancellationToken);
            
            await RecordChangeAsync(reportId, "WorkEntry", workEntry.Id, "Created", null, new { 
                workEntry.WorkTypeId, 
                workEntry.Quantity, 
                workEntry.Description, 
                workEntry.WorkerCount, 
                workEntry.HoursSpent,
                workEntry.OrderedWorkId,
                workEntry.IsAddedByForeman
            }, cancellationToken);
        }
        await dailyReportRepository.SaveChangesAsync(cancellationToken);

        return await GetReportResponseAsync(reportId, cancellationToken);
    }

    public async Task<ForemanDkpResponse> AddMaterialAsync(Guid reportId, AddForemanMaterialRequest request, Guid crewId, CancellationToken cancellationToken)
    {
        var report = await GetReportWithAccessCheckAsync(reportId, crewId, cancellationToken);

        if (report.Status != DailyReportStatus.Draft && report.Status != DailyReportStatus.Rejected)
            throw new ValidationException("Nie można edytować karty która nie jest szkicem lub nie wymaga poprawek.");

        var warehouse = await warehouseRepository.GetSubWarehouseByOwnerAsync(crewId, cancellationToken);
        
        var existingUsage = report.MaterialUsages.FirstOrDefault(mu => mu.MaterialId == request.MaterialId);
        if (existingUsage != null)
        {
            var oldQuantity = existingUsage.Quantity;
            var quantityDiff = request.Quantity - oldQuantity;
            
            if (warehouse != null && quantityDiff != 0)
            {
                if (quantityDiff > 0)
                {
                    await StockService.ReserveMaterialAsync(warehouseRepository, warehouse.Id, request.MaterialId, quantityDiff, cancellationToken);
                }
                else
                {
                    await StockService.ReleaseReservationAsync(warehouseRepository, warehouse.Id, request.MaterialId, Math.Abs(quantityDiff), cancellationToken);
                }
            }
            
            var oldValues = new { existingUsage.Quantity };
            existingUsage.Quantity = request.Quantity;
            var newValues = new { Quantity = request.Quantity };
            
            await RecordChangeAsync(reportId, "MaterialUsage", existingUsage.Id, "Updated", oldValues, newValues, cancellationToken);
        }
        else if (request.Quantity > 0)
        {
            if (warehouse != null)
            {
                await StockService.ReserveMaterialAsync(warehouseRepository, warehouse.Id, request.MaterialId, request.Quantity, cancellationToken);
            }
            
            var materialUsage = new MaterialUsage
            {
                Id = Guid.NewGuid(),
                DailyReportId = reportId,
                MaterialId = request.MaterialId,
                Quantity = request.Quantity
            };
            await dailyReportRepository.AddMaterialAsync(materialUsage, cancellationToken);
            
            await RecordChangeAsync(reportId, "MaterialUsage", materialUsage.Id, "Created", null, new { materialUsage.MaterialId, materialUsage.Quantity }, cancellationToken);
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

        await dailyReportRepository.AddDailyReportWorkOrderAsync(dailyReportWorkOrder, cancellationToken);
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

        await dailyReportRepository.RemoveDailyReportWorkOrderAsync(dailyReportWorkOrder, cancellationToken);
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

        var previousStatus = report.Status;
        DailyReportStatus nextStatus = DailyReportStatus.Submitted;
        
        if (previousStatus == DailyReportStatus.Rejected)
        {
            var lastRejection = report.StatusHistory
                .Where(h => h.ToStatus == DailyReportStatus.Rejected)
                .OrderByDescending(h => h.ChangedAt)
                .FirstOrDefault();
            
            if (lastRejection?.FromStatus == DailyReportStatus.SpmReview)
            {
                nextStatus = DailyReportStatus.SpmReview;
            }
            else if (lastRejection?.FromStatus == DailyReportStatus.SubcontractorReview)
            {
                nextStatus = DailyReportStatus.SubcontractorReview;
            }
        }

        report.Status = nextStatus;
        report.RejectionReason = null;
        
        var history = new DailyReportStatusHistory
        {
            DailyReportId = reportId,
            FromStatus = previousStatus,
            ToStatus = nextStatus,
            ChangedById = null,
            ChangedByWorkerId = currentUserContext.UserId,
            ChangedByEmail = currentUserContext.Email,
            ChangedAt = DateTime.UtcNow,
        };
        await dailyReportRepository.AddStatusHistoryAsync(history, cancellationToken);
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

    private async Task RecordChangeAsync(Guid reportId, string entryType, Guid entryId, string changeType, object? oldValues, object? newValues, CancellationToken cancellationToken)
    {
        var history = new DailyReportChangeHistory
        {
            DailyReportId = reportId,
            EntryType = entryType,
            EntryId = entryId,
            ChangeType = changeType,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            ChangedById = null, // Foreman is not in Users table
            ChangedByWorkerId = currentUserContext.UserId,
            ChangedByEmail = currentUserContext.Email,
            ChangedAt = DateTime.UtcNow
        };
        await dailyReportRepository.AddChangeHistoryAsync(history, cancellationToken);
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
                Hours = wh.Hours,
                IsAbsent = wh.IsAbsent
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
                Description = we.Description,
                WorkerCount = we.WorkerCount,
                HoursSpent = we.HoursSpent,
                IsAddedByForeman = we.IsAddedByForeman
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
                    AuthorEmail = c.Author?.Email ?? c.SubcontractorWorker?.Email ?? "Nieznany",
                    AuthorRole = c.Author?.Role.ToString() ?? (c.SubcontractorWorker != null ? "SubcontractorForeman" : "Nieznany"),
                    Content = c.Content,
                    IsResolved = c.IsResolved,
                    CreatedAt = c.CreatedAt,
                    Replies = c.Replies.Select(r => new ForemanCommentReply
                    {
                        Id = r.Id,
                        AuthorEmail = r.Author?.Email ?? r.SubcontractorWorker?.Email ?? "Nieznany",
                        AuthorRole = r.Author?.Role.ToString() ?? (r.SubcontractorWorker != null ? "SubcontractorForeman" : "Nieznany"),
                        Content = r.Content,
                        CreatedAt = r.CreatedAt
                    }).OrderBy(r => r.CreatedAt).ToList()
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
            }).ToList(),
            ChangeHistory = report.ChangeHistory.Select(ch => new ForemanChangeHistoryItem
            {
                Id = ch.Id,
                EntryType = ch.EntryType,
                EntryId = ch.EntryId,
                ChangeType = ch.ChangeType,
                OldValues = ch.OldValues,
                NewValues = ch.NewValues,
                ChangedByEmail = ch.ChangedByEmail,
                ChangedAt = ch.ChangedAt
            }).OrderByDescending(ch => ch.ChangedAt).ToList()
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
    public List<ForemanChangeHistoryItem> ChangeHistory { get; set; } = [];
}

public class ForemanChangeHistoryItem
{
    public Guid Id { get; set; }
    public required string EntryType { get; set; }
    public Guid EntryId { get; set; }
    public required string ChangeType { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedByEmail { get; set; }
    public DateTime ChangedAt { get; set; }
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
    public List<ForemanCommentReply> Replies { get; set; } = [];
}

public class ForemanCommentReply
{
    public Guid Id { get; set; }
    public required string AuthorEmail { get; set; }
    public required string AuthorRole { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ForemanWorkHoursItem
{
    public Guid Id { get; set; }
    public Guid WorkerId { get; set; }
    public required string WorkerName { get; set; }
    public decimal Hours { get; set; }
    public bool IsAbsent { get; set; }
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
    public int WorkerCount { get; set; }
    public decimal HoursSpent { get; set; }
    public bool IsAddedByForeman { get; set; }
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

public record AddForemanHoursRequest(Guid WorkerId, decimal Hours, bool IsAbsent = false);
public record AddForemanWorkRequest(Guid WorkTypeId, decimal Quantity, string? Description, Guid? OrderedWorkId, int WorkerCount = 0, decimal HoursSpent = 0);
public record AddForemanMaterialRequest(Guid MaterialId, decimal Quantity);

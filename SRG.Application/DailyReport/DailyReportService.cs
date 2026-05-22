using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using SRG.Application.Audit;
using SRG.Application.Common;
using SRG.Application.Persistence;
using SRG.Application.Warehouses;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.DailyReports;

public class DailyReportService(
    IDailyReportRepository dailyReportRepository,
    IConstructionRepository constructionRepository,
    IWarehouseRepository warehouseRepository,
    IWorkOrderRepository workOrderRepository,
    IAuditService auditService,
    ICurrentUserContext currentUserContext,
    IOptions<DailyReportSettings> dailyReportSettings) : IDailyReportService
{
    public async Task<DailyReportResponse> CreateDailyReportAsync(
        CreateDailyReportRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default)
    {
        if (await dailyReportRepository.ExistsForCrewDateAsync(request.CrewId, request.Date, cancellationToken))
        {
            throw new ValidationException("DailyReport already exists for this Crew and date.");
        }

        var crew = await constructionRepository.GetCrewByIdAsync(request.CrewId, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        var sections = await constructionRepository.GetSectionsByProjectAsync(crew.ProjectId, cancellationToken);
        var section = sections.FirstOrDefault(section => section.Id == request.SectionId)
            ?? throw new KeyNotFoundException("Section was not found in Crew project.");

        if (request.WorkOrderId is not null)
        {
            await ValidateWorkOrderAssignmentAsync(request.WorkOrderId.Value, crew.Id, crew.ProjectId, cancellationToken);
        }

        var dailyReport = new Domain.Entities.DailyReport
        {
            Date = request.Date,
            CrewId = crew.Id,
            ProjectId = crew.ProjectId,
            SectionId = section.Id,
            WorkOrderId = request.WorkOrderId,
            CreatedById = createdById,
            Status = DailyReportStatus.Draft,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        await dailyReportRepository.AddAsync(dailyReport, cancellationToken);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(createdById, "CREATE_DAILY_REPORT", "DailyReport", dailyReport.Id, new
        {
            dailyReport.Date,
            dailyReport.CrewId,
            dailyReport.ProjectId,
            dailyReport.SectionId,
        }, cancellationToken);

        return ToResponse(dailyReport);
    }

    public async Task<List<DailyReportResponse>> GetByCrewAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        var reports = await dailyReportRepository.GetByCrewAsync(crewId, cancellationToken);
        return reports.Select(ToResponse).ToList();
    }

    public async Task<List<DailyReportResponse>> GetByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var reports = await dailyReportRepository.GetByWorkOrderAsync(workOrderId, cancellationToken);
        return reports.Select(ToResponse).ToList();
    }

    public async Task<List<DailyReportResponse>> GetSubmittedAsync(CancellationToken cancellationToken = default)
    {
        var reports = await dailyReportRepository.GetByStatusAsync(DailyReportStatus.Submitted, cancellationToken);
        return reports.Select(ToResponse).ToList();
    }

    public async Task<DailyReportResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ToResponse(await GetReportAsync(id, cancellationToken));
    }

    public async Task<DailyReportResponse> UpdateNotesAsync(
        Guid id,
        UpdateDailyReportNotesRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = await GetEditableReportAsync(id, cancellationToken);
        report.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(report);
    }

    public async Task<DailyReportResponse> UpdateWorkOrderAsync(
        Guid id,
        UpdateDailyReportWorkOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = await GetEditableReportAsync(id, cancellationToken);
        if (request.WorkOrderId is not null && report.CrewId.HasValue && report.ProjectId.HasValue)
        {
            await ValidateWorkOrderAssignmentAsync(request.WorkOrderId.Value, report.CrewId.Value, report.ProjectId.Value, cancellationToken);
        }

        report.WorkOrderId = request.WorkOrderId;
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(await GetReportAsync(id, cancellationToken));
    }

    public async Task<DailyReportResponse> AddWorkOrderAsync(
        Guid id,
        AddDailyReportWorkOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = await GetEditableReportAsync(id, cancellationToken);
        
        var workOrder = await workOrderRepository.GetWorkOrderByIdAsync(request.WorkOrderId, cancellationToken)
            ?? throw new KeyNotFoundException("WorkOrder was not found.");

        if (report.DailyReportWorkOrders.Any(drwo => drwo.WorkOrderId == request.WorkOrderId))
        {
            throw new ValidationException("WorkOrder is already added to this DailyReport.");
        }

        var dailyReportWorkOrder = new Domain.Entities.DailyReportWorkOrder
        {
            DailyReportId = report.Id,
            WorkOrderId = request.WorkOrderId,
            AddedAt = DateTime.UtcNow,
        };

        report.DailyReportWorkOrders.Add(dailyReportWorkOrder);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(await GetReportAsync(id, cancellationToken));
    }

    public async Task<DailyReportResponse> RemoveWorkOrderAsync(
        Guid id,
        Guid workOrderId,
        CancellationToken cancellationToken = default)
    {
        var report = await GetEditableReportAsync(id, cancellationToken);
        
        var dailyReportWorkOrder = report.DailyReportWorkOrders.FirstOrDefault(drwo => drwo.WorkOrderId == workOrderId)
            ?? throw new KeyNotFoundException("WorkOrder is not associated with this DailyReport.");

        report.DailyReportWorkOrders.Remove(dailyReportWorkOrder);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(await GetReportAsync(id, cancellationToken));
    }

    public async Task<DailyReportResponse> AddWorkHoursAsync(
        Guid id,
        AddWorkHourRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = await GetEditableReportAsync(id, cancellationToken);

        if (request.Hours <= 0)
        {
            throw new ValidationException("Hours must be greater than zero.");
        }

        await ValidateWorkerAssignmentAsync(report, request, cancellationToken);

        await dailyReportRepository.AddWorkHoursAsync(new WorkHour
        {
            DailyReportId = report.Id,
            WorkerId = request.WorkerId,
            SubcontractorWorkerId = request.SubcontractorWorkerId,
            Hours = request.Hours,
        }, cancellationToken);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(await GetReportAsync(id, cancellationToken));
    }

    public async Task<DailyReportResponse> AddWorkAsync(
        Guid id,
        AddWorkEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = await GetEditableReportAsync(id, cancellationToken);

        if (request.Quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }

        await ValidateWorkEntryAsync(report, request, cancellationToken);

        await dailyReportRepository.AddWorkEntryAsync(new WorkEntry
        {
            DailyReportId = report.Id,
            WorkTypeId = request.WorkTypeId,
            OrderedWorkId = request.OrderedWorkId,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Quantity = request.Quantity,
        }, cancellationToken);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(await GetReportAsync(id, cancellationToken));
    }

    public async Task<DailyReportResponse> AddMaterialAsync(
        Guid id,
        AddMaterialUsageRequest request,
        Guid foremanId,
        CancellationToken cancellationToken = default)
    {
        var report = await GetEditableReportAsync(id, cancellationToken);

        if (request.Quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }

        _ = await warehouseRepository.GetMaterialByIdAsync(request.MaterialId, cancellationToken)
            ?? throw new KeyNotFoundException("Material was not found.");
        await ValidateMaterialUsageAsync(report, request, cancellationToken);

        await warehouseRepository.ExecuteInTransactionAsync(async () =>
        {
            var subWarehouse = await WarehouseService.EnsureSubWarehouseAsync(
                warehouseRepository,
                foremanId,
                cancellationToken);

            // Reserve materials instead of consuming them immediately
            await StockService.ReserveMaterialAsync(
                warehouseRepository,
                subWarehouse.Id,
                request.MaterialId,
                request.Quantity,
                cancellationToken);

            await dailyReportRepository.AddMaterialAsync(new MaterialUsage
            {
                DailyReportId = report.Id,
                MaterialId = request.MaterialId,
                OrderedMaterialId = request.OrderedMaterialId,
                Quantity = request.Quantity,
            }, cancellationToken);

            await dailyReportRepository.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return ToResponse(await GetReportAsync(id, cancellationToken));
    }

    public async Task<DailyReportResponse> SubmitDailyReportAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await GetEditableReportAsync(id, cancellationToken);
        EnsureHasEntries(report);
        var previousStatus = report.Status;
        
        // Determine next status based on rejection source
        DailyReportStatus nextStatus = DailyReportStatus.Submitted;
        if (previousStatus == DailyReportStatus.Rejected)
        {
            // Check status history to find what status was before rejection
            var lastRejection = report.StatusHistory
                .Where(h => h.ToStatus == DailyReportStatus.Rejected)
                .OrderByDescending(h => h.ChangedAt)
                .FirstOrDefault();
            
            if (lastRejection?.FromStatus == DailyReportStatus.SpmReview)
            {
                // If rejected from SpmReview, go back to SpmReview (directly to SPM)
                nextStatus = DailyReportStatus.SpmReview;
            }
            else if (lastRejection?.FromStatus == DailyReportStatus.SubcontractorReview)
            {
                // If rejected from SubcontractorReview, go back to SubcontractorReview (directly to Subcontractor)
                nextStatus = DailyReportStatus.SubcontractorReview;
            }
        }
        
        report.Status = nextStatus;
        report.RejectionReason = null;
        await RecordStatusChangeAsync(report.Id, previousStatus, report.Status, null, cancellationToken);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? report.CreatedById ?? Guid.Empty, "SUBMIT_DAILY_REPORT", "DailyReport", report.Id, new
        {
            report.Status,
        }, cancellationToken);
        return ToResponse(report);
    }

    public async Task<DailyReportResponse> RejectDailyReportAsync(
        Guid id,
        RejectDailyReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = await GetReportAsync(id, cancellationToken);

        if (report.Status is not DailyReportStatus.Submitted 
            and not DailyReportStatus.PmReview 
            and not DailyReportStatus.SpmReview
            and not DailyReportStatus.SubcontractorReview
            and not DailyReportStatus.SubcontractorRejected)  // PM can forward Subco rejection to foreman
        {
            throw new ValidationException("Only Submitted or review-stage DailyReport can be rejected.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException("Rejection reason is required.");
        }

        var previousStatus = report.Status;
        
        // When Subcontractor rejects from SubcontractorReview → SubcontractorRejected (PM sees)
        // When PM/SPM rejects (including from SubcontractorRejected) → Rejected (foreman sees)
        report.Status = previousStatus == DailyReportStatus.SubcontractorReview 
            ? DailyReportStatus.SubcontractorRejected 
            : DailyReportStatus.Rejected;
        report.RejectionReason = request.Reason.Trim();
        await RecordStatusChangeAsync(report.Id, previousStatus, report.Status, request.Reason.Trim(), cancellationToken);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? Guid.Empty, "REJECT_DAILY_REPORT", "DailyReport", report.Id, new
        {
            report.Status,
            report.RejectionReason,
        }, cancellationToken);
        return ToResponse(report);
    }

    public async Task<List<DailyReportResponse>> GetForPmReviewAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new[]
        {
            DailyReportStatus.Submitted,
            DailyReportStatus.PmReview,
            DailyReportStatus.SpmReview,
            DailyReportStatus.SpmApproved,
            DailyReportStatus.SubcontractorReview,
            DailyReportStatus.SubcontractorApproved,
            DailyReportStatus.Rejected
        };
        
        var allReports = await dailyReportRepository.GetByStatusesAsync(statuses, cancellationToken);
        
        // Filter out rejected reports that were NOT rejected from PM review
        var result = allReports.Where(r =>
        {
            if (r.Status != DailyReportStatus.Rejected) return true;
            
            var lastRejection = r.StatusHistory
                .Where(h => h.ToStatus == DailyReportStatus.Rejected)
                .OrderByDescending(h => h.ChangedAt)
                .FirstOrDefault();
            return lastRejection?.FromStatus != DailyReportStatus.SpmReview && 
                   lastRejection?.FromStatus != DailyReportStatus.SubcontractorReview;
        }).ToList();
        
        return result.Select(ToResponse).ToList();
    }

    public async Task<List<DailyReportResponse>> GetForSpmReviewAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new[] { DailyReportStatus.SpmReview, DailyReportStatus.SpmApproved, DailyReportStatus.Rejected };
        var allReports = await dailyReportRepository.GetByStatusesAsync(statuses, cancellationToken);
        
        // Filter rejected reports to only those rejected from SpmReview
        var result = allReports.Where(r =>
        {
            if (r.Status != DailyReportStatus.Rejected) return true;
            
            return r.StatusHistory.Any(h => h.ToStatus == DailyReportStatus.Rejected && h.FromStatus == DailyReportStatus.SpmReview) &&
                   r.StatusHistory.OrderByDescending(h => h.ChangedAt).First().ToStatus == DailyReportStatus.Rejected;
        }).ToList();
        
        return result.Select(ToResponse).ToList();
    }

    public async Task<List<DailyReportResponse>> GetForSubcontractorReviewAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.UserId.HasValue)
            return [];

        // Get Subcontractor's crews
        var subcontractorCrews = await constructionRepository.GetSubcontractorCrewsAsync(currentUserContext.UserId.Value, cancellationToken);
        var crewIds = subcontractorCrews.Select(c => c.Id).ToHashSet();
        
        if (crewIds.Count == 0)
            return [];

        // Get all reports in relevant statuses in ONE query
        var statuses = new[]
        {
            DailyReportStatus.Submitted,
            DailyReportStatus.PmReview,
            DailyReportStatus.SpmReview,
            DailyReportStatus.SubcontractorReview,
            DailyReportStatus.SubcontractorApproved,
            DailyReportStatus.Rejected
        };
        
        var allReports = await dailyReportRepository.GetByStatusesAsync(statuses, cancellationToken);
        
        // Filter to only this Subcontractor's crews
        var result = allReports
            .Where(r => r.SubcontractorCrewId.HasValue && crewIds.Contains(r.SubcontractorCrewId.Value))
            .ToList();
        
        return result.Select(ToResponse).ToList();
    }

    public async Task<List<DailyReportListItemResponse>> GetForPmReviewListAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new[]
        {
            DailyReportStatus.Submitted,
            DailyReportStatus.PmReview,
            DailyReportStatus.SpmReview,
            DailyReportStatus.SpmApproved,
            DailyReportStatus.SubcontractorReview,
            DailyReportStatus.SubcontractorApproved,
            DailyReportStatus.Rejected,
            DailyReportStatus.SubcontractorRejected  // PM sees Subcontractor rejections
        };
        
        var allItems = await dailyReportRepository.GetListItemsByStatusesAsync(statuses, cancellationToken);
        
        // Filter out Rejected reports that were rejected by SPM (those go back to foreman, not PM)
        return allItems.Where(r =>
        {
            if (r.Status != DailyReportStatus.Rejected) return true;
            return r.RejectedFromStatus != DailyReportStatus.SpmReview;
        }).ToList();
    }

    public async Task<List<DailyReportListItemResponse>> GetForSpmReviewListAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new[] { DailyReportStatus.SpmReview, DailyReportStatus.SpmApproved, DailyReportStatus.Rejected };
        var allItems = await dailyReportRepository.GetListItemsByStatusesAsync(statuses, cancellationToken);
        
        // Filter rejected reports to only those rejected from SpmReview
        return allItems.Where(r =>
        {
            if (r.Status != DailyReportStatus.Rejected) return true;
            return r.RejectedFromStatus == DailyReportStatus.SpmReview;
        }).ToList();
    }

    public async Task<List<DailyReportListItemResponse>> GetForSubcontractorReviewListAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.UserId.HasValue)
            return [];

        // Get Subcontractor's crews
        var subcontractorCrews = await constructionRepository.GetSubcontractorCrewsAsync(currentUserContext.UserId.Value, cancellationToken);
        var crewIds = subcontractorCrews.Select(c => c.Id).ToHashSet();
        
        if (crewIds.Count == 0)
            return [];

        var statuses = new[]
        {
            DailyReportStatus.Submitted,
            DailyReportStatus.PmReview,
            DailyReportStatus.SpmReview,
            DailyReportStatus.SubcontractorReview,
            DailyReportStatus.SubcontractorApproved,
            DailyReportStatus.Rejected
        };
        
        var allItems = await dailyReportRepository.GetListItemsByStatusesAsync(statuses, cancellationToken);
        
        // Filter to only this Subcontractor's crews
        return allItems
            .Where(r => r.SubcontractorCrewId.HasValue && crewIds.Contains(r.SubcontractorCrewId.Value))
            .ToList();
    }

    public async Task<List<DailyReportCalendarResponse>> GetCalendarAsync(
        Guid crewId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var reports = await dailyReportRepository.GetByCrewDateRangeAsync(crewId, startDate, endDate, cancellationToken);

        return reports.Select(r => new DailyReportCalendarResponse(
            r.Id,
            r.Date,
            r.Status,
            r.WorkHours.Count > 0,
            r.WorkEntries.Count > 0,
            r.MaterialUsages.Count > 0,
            r.Comments.Any(c => !c.IsResolved && c.ParentCommentId is null),
            r.Comments.Count(c => !c.IsResolved && c.ParentCommentId is null))).ToList();
    }

    public async Task<DailyReportResponse> AddCommentAsync(
        Guid id,
        AddDailyReportCommentRequest request,
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        var report = await GetReportAsync(id, cancellationToken);

        if (report.Status is DailyReportStatus.Draft or DailyReportStatus.SpmApproved or DailyReportStatus.SubcontractorApproved)
        {
            throw new ValidationException("Comments can only be added during review stages.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ValidationException("Comment content is required.");
        }

        if (request.ParentCommentId is not null)
        {
            var parentComment = report.Comments.FirstOrDefault(c => c.Id == request.ParentCommentId)
                ?? throw new KeyNotFoundException("Parent comment was not found.");

            if (parentComment.IsResolved)
            {
                throw new ValidationException("Cannot reply to resolved comment.");
            }
        }

        var role = currentUserContext.Role ?? "Unknown";
        var isSubcontractorForeman = role is "SubcontractorForeman" or "Foreman";
        
        var comment = new DailyReportComment
        {
            DailyReportId = report.Id,
            Section = request.Section,
            RecordId = request.RecordId,
            AuthorId = isSubcontractorForeman ? null : authorId,
            SubcontractorWorkerId = isSubcontractorForeman ? authorId : null,
            AuthorEmail = currentUserContext.Email ?? string.Empty,
            AuthorRole = role,
            Content = request.Content.Trim(),
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTime.UtcNow,
        };

        await dailyReportRepository.AddCommentAsync(comment, cancellationToken);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(await GetReportAsync(id, cancellationToken));
    }

    public async Task<DailyReportResponse> ResolveCommentAsync(
        Guid id,
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var role = currentUserContext.Role;
        if (role is not "PM" and not "SPM" and not "Subcontractor")
        {
            throw new ValidationException("Tylko PM lub Podwykonawca może oznaczyć komentarz jako rozwiązany.");
        }

        var report = await GetReportAsync(id, cancellationToken);
        var comment = report.Comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw new KeyNotFoundException("Comment was not found.");

        comment.IsResolved = true;
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(await GetReportAsync(id, cancellationToken));
    }

    public async Task<DailyReportResponse> UnresolveCommentAsync(
        Guid id,
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var report = await GetReportAsync(id, cancellationToken);
        var comment = report.Comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw new KeyNotFoundException("Comment was not found.");

        comment.IsResolved = false;
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(await GetReportAsync(id, cancellationToken));
    }

    public async Task<DailyReportResponse> SendBackToForemanAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await GetReportAsync(id, cancellationToken);

        if (report.Status is not DailyReportStatus.Submitted and not DailyReportStatus.PmApproved)
        {
            throw new ValidationException("Can only send back Submitted or PmApproved DailyReports.");
        }

        var previousStatus = report.Status;
        report.Status = previousStatus == DailyReportStatus.Submitted ? DailyReportStatus.PmReview : DailyReportStatus.SpmReview;

        await RecordStatusChangeAsync(report.Id, previousStatus, report.Status, null, cancellationToken);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? Guid.Empty, "SEND_BACK_DAILY_REPORT", "DailyReport", report.Id, new
        {
            PreviousStatus = previousStatus,
            NewStatus = report.Status,
        }, cancellationToken);
        return ToResponse(report);
    }

    public async Task<DailyReportResponse> PmApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await GetReportAsync(id, cancellationToken);

        // PM can approve: Submitted, PmReview, or SubcontractorRejected (re-send to Subco)
        if (report.Status is not DailyReportStatus.Submitted 
            and not DailyReportStatus.PmReview 
            and not DailyReportStatus.SubcontractorRejected)
        {
            throw new ValidationException("Only Submitted, PmReview, or SubcontractorRejected DailyReport can be approved by PM.");
        }

        EnsureHasEntries(report);

        var unresolvedComments = report.Comments.Where(c => !c.IsResolved && c.ParentCommentId is null).ToList();
        if (unresolvedComments.Count > 0)
        {
            var details = string.Join(", ", unresolvedComments.Select(c => $"[{c.Section}/{c.RecordId?.ToString()[..8] ?? "ogólny"}: {c.Content[..Math.Min(20, c.Content.Length)]}]"));
            throw new ValidationException($"Nierozwiązane komentarze ({unresolvedComments.Count}): {details}");
        }

        var previousStatus = report.Status;
        
        // If SubcontractorRejected, always go back to SubcontractorReview
        // Otherwise: SubcontractorCrew → SubcontractorReview, regular crew → SpmReview
        if (previousStatus == DailyReportStatus.SubcontractorRejected)
        {
            report.Status = DailyReportStatus.SubcontractorReview;
        }
        else
        {
            report.Status = report.SubcontractorCrewId.HasValue 
                ? DailyReportStatus.SubcontractorReview 
                : DailyReportStatus.SpmReview;
        }
        report.RejectionReason = null;
        await RecordStatusChangeAsync(report.Id, previousStatus, report.Status, null, cancellationToken);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? Guid.Empty, "PM_APPROVE_DAILY_REPORT", "DailyReport", report.Id, new
        {
            report.Status,
        }, cancellationToken);
        return ToResponse(report);
    }

    public async Task<DailyReportResponse> SpmApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await GetReportAsync(id, cancellationToken);

        if (report.Status is not DailyReportStatus.SpmReview)
        {
            throw new ValidationException("Tylko karty oczekujące na weryfikację Subco mogą być zatwierdzone.");
        }

        EnsureHasEntries(report);

        var unresolvedComments = report.Comments.Where(c => !c.IsResolved && c.ParentCommentId is null).ToList();
        if (unresolvedComments.Count > 0)
        {
            var details = string.Join(", ", unresolvedComments.Select(c => $"[{c.Section}/{c.RecordId?.ToString()[..8] ?? "ogólny"}: {c.Content[..Math.Min(20, c.Content.Length)]}]"));
            throw new ValidationException($"Nierozwiązane komentarze ({unresolvedComments.Count}): {details}");
        }

        var previousStatus = report.Status;
        report.Status = DailyReportStatus.SpmApproved;
        
        // Consume materials from crew's warehouse
        if (report.MaterialUsages.Count > 0 && report.CrewId.HasValue)
        {
            var subWarehouse = await warehouseRepository.GetSubWarehouseByOwnerAsync(report.CrewId.Value, cancellationToken);
            
            if (subWarehouse != null)
            {
                foreach (var usage in report.MaterialUsages)
                {
                    await StockService.ConsumeReservedMaterialAsync(
                        warehouseRepository,
                        subWarehouse.Id,
                        usage.MaterialId,
                        usage.Quantity,
                        StockMovementSourceType.DailyReportUsage,
                        report.Id,
                        currentUserContext.UserId ?? Guid.Empty,
                        cancellationToken);
                }
            }
        }
        
        await RecordStatusChangeAsync(report.Id, previousStatus, report.Status, null, cancellationToken);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? Guid.Empty, "SPM_APPROVE_DAILY_REPORT", "DailyReport", report.Id, new
        {
            report.Status,
        }, cancellationToken);
        return ToResponse(report);
    }

    public async Task<DailyReportResponse> SubcontractorApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await GetReportAsync(id, cancellationToken);

        // Accept both SubcontractorReview and SpmReview (legacy status for SubcontractorCrew reports)
        if (report.Status is not DailyReportStatus.SubcontractorReview and not DailyReportStatus.SpmReview)
        {
            throw new ValidationException("Tylko karty oczekujące na weryfikację Podwykonawcy mogą być zatwierdzone.");
        }

        // Verify the Subcontractor owns this crew
        if (!currentUserContext.UserId.HasValue || !report.SubcontractorCrewId.HasValue)
        {
            throw new ValidationException("Brak dostępu do tej karty.");
        }

        var subcontractorCrews = await constructionRepository.GetSubcontractorCrewsAsync(currentUserContext.UserId.Value, cancellationToken);
        if (!subcontractorCrews.Any(c => c.Id == report.SubcontractorCrewId.Value))
        {
            throw new ValidationException("Ta karta nie należy do Twoich brygad.");
        }

        EnsureHasEntries(report);

        var unresolvedComments = report.Comments.Where(c => !c.IsResolved && c.ParentCommentId is null).ToList();
        if (unresolvedComments.Count > 0)
        {
            var details = string.Join(", ", unresolvedComments.Select(c => $"[{c.Section}/{c.RecordId?.ToString()[..8] ?? "ogólny"}: {c.Content[..Math.Min(20, c.Content.Length)]}]"));
            throw new ValidationException($"Nierozwiązane komentarze ({unresolvedComments.Count}): {details}");
        }

        var previousStatus = report.Status;
        report.Status = DailyReportStatus.SubcontractorApproved;
        
        // Consume materials from crew's warehouse
        if (report.MaterialUsages.Count > 0 && report.SubcontractorCrewId.HasValue)
        {
            var subWarehouse = await warehouseRepository.GetSubWarehouseByOwnerAsync(report.SubcontractorCrewId.Value, cancellationToken);
            
            if (subWarehouse != null)
            {
                foreach (var usage in report.MaterialUsages)
                {
                    await StockService.ConsumeReservedMaterialAsync(
                        warehouseRepository,
                        subWarehouse.Id,
                        usage.MaterialId,
                        usage.Quantity,
                        StockMovementSourceType.DailyReportUsage,
                        report.Id,
                        currentUserContext.UserId ?? Guid.Empty,
                        cancellationToken);
                }
            }
        }
        
        await RecordStatusChangeAsync(report.Id, previousStatus, report.Status, null, cancellationToken);
        await dailyReportRepository.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? Guid.Empty, "SUBCONTRACTOR_APPROVE_DAILY_REPORT", "DailyReport", report.Id, new
        {
            report.Status,
        }, cancellationToken);
        return ToResponse(report);
    }

    private async Task<Domain.Entities.DailyReport> GetEditableReportAsync(Guid id, CancellationToken cancellationToken)
    {
        var report = await GetReportAsync(id, cancellationToken);
        var lockReason = GetEditLockReason(report);

        if (lockReason is not null)
        {
            throw new ValidationException(lockReason);
        }

        return report;
    }

    private async Task<Domain.Entities.DailyReport> GetReportAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dailyReportRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("DailyReport was not found.");
    }

    private static void EnsureHasEntries(Domain.Entities.DailyReport report)
    {
        if (report.WorkHours.Count == 0 && report.WorkEntries.Count == 0 && report.MaterialUsages.Count == 0)
        {
            throw new ValidationException("DailyReport cannot be submitted or approved without entries.");
        }
    }

    private DailyReportResponse ToResponse(Domain.Entities.DailyReport report)
    {
        var topLevelComments = report.Comments
            .Where(c => c.ParentCommentId is null)
            .Select(ToCommentResponse)
            .ToList();

        return new DailyReportResponse(
            report.Id,
            report.Date,
            report.CrewId,
            report.Crew?.Name,
            report.SubcontractorCrewId,
            report.SubcontractorCrew?.Name,
            report.ProjectId,
            report.SectionId,
            report.WorkOrderId,
            report.CreatedById,
            report.Status,
            report.Notes,
            report.RejectionReason,
            report.CreatedAt,
            GetEditLockReason(report) is null,
            GetEditLockReason(report),
            report.Comments.Any(c => !c.IsResolved && c.ParentCommentId is null),
            report.WorkHours.Select(entry => new WorkHourResponse(
                entry.Id,
                entry.DailyReportId,
                entry.WorkerId,
                entry.SubcontractorWorkerId,
                GetWorkerName(entry),
                entry.Hours)).ToList(),
            report.WorkEntries.Select(entry => new WorkEntryResponse(
                entry.Id,
                entry.DailyReportId,
                entry.WorkTypeId,
                entry.OrderedWorkId,
                entry.OrderedWork?.WorkOrderId,
                entry.OrderedWork?.WorkOrder?.Number,
                entry.WorkType?.Name,
                entry.WorkType?.Code,
                entry.WorkType?.Unit,
                entry.Description,
                entry.Quantity,
                entry.OrderedWork?.PlannedQuantity,
                entry.WorkerCount,
                entry.HoursSpent,
                entry.IsAddedByForeman)).ToList(),
            report.MaterialUsages.Select(entry => new MaterialUsageResponse(
                entry.Id,
                entry.DailyReportId,
                entry.MaterialId,
                entry.OrderedMaterialId,
                entry.Material?.Name,
                entry.Material?.Unit,
                entry.Quantity)).ToList(),
            topLevelComments,
            report.StatusHistory.OrderByDescending(h => h.ChangedAt).Select(h => new DailyReportStatusHistoryResponse(
                h.Id,
                h.FromStatus,
                h.ToStatus,
                h.Reason,
                h.ChangedById,
                h.ChangedBy?.Email,
                h.ChangedAt)).ToList(),
            report.ChangeHistory.OrderByDescending(h => h.ChangedAt).Select(h => new DailyReportChangeHistoryResponse(
                h.Id,
                h.EntryType,
                h.EntryId,
                h.ChangeType,
                h.OldValues,
                h.NewValues,
                h.ChangedById ?? h.ChangedByWorkerId,
                h.ChangedBy?.Email ?? h.ChangedByEmail,
                h.ChangedAt)).ToList(),
            report.DailyReportWorkOrders.Select(drwo => new DailyReportWorkOrderResponse(
                drwo.WorkOrderId,
                drwo.WorkOrder?.Number ?? "",
                drwo.WorkOrder?.Description,
                drwo.WorkOrder?.OrderedWorks.Select(ow => new OrderedWorkSummary(
                    ow.Id,
                    ow.WorkTypeId,
                    ow.WorkType?.Name,
                    ow.WorkType?.Code,
                    ow.Unit,
                    ow.PlannedQuantity,
                    ow.Description)).ToList() ?? [])).ToList());
    }

    private DailyReportCommentResponse ToCommentResponse(DailyReportComment comment)
    {
        // Use denormalized fields if available, fallback to navigation properties for legacy data
        var authorEmail = !string.IsNullOrEmpty(comment.AuthorEmail) 
            ? comment.AuthorEmail 
            : comment.Author?.Email ?? comment.SubcontractorWorker?.Email ?? "Unknown";
        var authorRole = !string.IsNullOrEmpty(comment.AuthorRole) 
            ? comment.AuthorRole 
            : comment.Author?.Role.ToString() ?? (comment.SubcontractorWorkerId.HasValue ? "SubcontractorForeman" : "Unknown");
            
        return new DailyReportCommentResponse(
            comment.Id,
            comment.Section,
            comment.RecordId,
            comment.AuthorId,
            comment.SubcontractorWorkerId,
            authorEmail,
            authorRole,
            comment.Content,
            comment.ParentCommentId,
            comment.IsResolved,
            comment.CreatedAt,
            comment.Replies.Select(ToCommentResponse).ToList());
    }

    private string? GetEditLockReason(Domain.Entities.DailyReport report)
    {
        if (report.Status is DailyReportStatus.PmApproved or DailyReportStatus.SpmApproved)
        {
            return "Approved DailyReports cannot be edited.";
        }

        if (report.Status == DailyReportStatus.Submitted)
        {
            return "Submitted DailyReports cannot be edited while pending review.";
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oldestEditableDate = today.AddDays(-dailyReportSettings.Value.EditableDaysBack);

        if (report.Date < oldestEditableDate)
        {
            return $"DailyReports older than {dailyReportSettings.Value.EditableDaysBack} days cannot be edited.";
        }

        if (report.Status is DailyReportStatus.PmReview or DailyReportStatus.SpmReview)
        {
            return null;
        }

        return null;
    }

    private static string? GetWorkerName(Domain.Entities.WorkHour entry)
    {
        if (entry.Worker != null)
            return $"{entry.Worker.FirstName} {entry.Worker.LastName}";
        if (entry.SubcontractorWorker != null)
            return $"{entry.SubcontractorWorker.FirstName} {entry.SubcontractorWorker.LastName}";
        return null;
    }

    private async Task ValidateWorkerAssignmentAsync(
        Domain.Entities.DailyReport report,
        AddWorkHourRequest request,
        CancellationToken cancellationToken)
    {
        var hasCrewWorker = request.WorkerId is not null;
        var hasSubcontractorWorker = request.SubcontractorWorkerId is not null;

        if (hasCrewWorker == hasSubcontractorWorker)
        {
            throw new ValidationException("Exactly one worker reference must be provided.");
        }

        if (request.WorkerId is not null)
        {
            var worker = await constructionRepository.GetWorkerByIdAsync(request.WorkerId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Worker was not found.");

            if (worker.CrewId != report.CrewId)
            {
                throw new ValidationException("Worker must belong to the DailyReport Crew.");
            }

            return;
        }

        var subcontractorWorker = await constructionRepository.GetSubcontractorWorkerByIdAsync(
            request.SubcontractorWorkerId!.Value,
            cancellationToken)
            ?? throw new KeyNotFoundException("Subcontractor worker was not found.");

        // Skip project assignment validation for subcontractor-created reports (ProjectId is null)
        if (report.ProjectId.HasValue)
        {
            var assignment = await constructionRepository.GetProjectSubcontractorAsync(
                report.ProjectId.Value,
                subcontractorWorker.SubcontractorId,
                cancellationToken);

            if (assignment is null)
            {
                throw new ValidationException("Subcontractor worker must belong to a subcontractor assigned to the DailyReport Project.");
            }
        }
    }

    private async Task ValidateWorkOrderAssignmentAsync(
        Guid workOrderId,
        Guid crewId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var workOrder = await workOrderRepository.GetWorkOrderByIdAsync(workOrderId, cancellationToken)
            ?? throw new KeyNotFoundException("WorkOrder was not found.");

        if (workOrder.ProjectId != projectId)
        {
            throw new ValidationException("WorkOrder must belong to the DailyReport Project.");
        }

        if (workOrder.CrewId is not null && workOrder.CrewId != crewId)
        {
            throw new ValidationException("WorkOrder is assigned to a different Crew.");
        }
    }

    private async Task ValidateWorkEntryAsync(
        Domain.Entities.DailyReport report,
        AddWorkEntryRequest request,
        CancellationToken cancellationToken)
    {
        var workType = await workOrderRepository.GetWorkTypeByIdAsync(request.WorkTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("WorkType was not found.");
        if (!workType.IsActive)
        {
            throw new ValidationException("WorkType must be active.");
        }

        if (request.OrderedWorkId is null)
        {
            return;
        }

        var orderedWork = await workOrderRepository.GetOrderedWorkByIdAsync(request.OrderedWorkId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("OrderedWork was not found.");
        if (orderedWork.WorkOrderId != report.WorkOrderId)
        {
            throw new ValidationException("OrderedWork must belong to the selected WorkOrder.");
        }

        if (orderedWork.WorkTypeId != request.WorkTypeId)
        {
            throw new ValidationException("WorkEntry WorkType must match OrderedWork WorkType.");
        }
    }

    private async Task ValidateMaterialUsageAsync(
        Domain.Entities.DailyReport report,
        AddMaterialUsageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.OrderedMaterialId is null)
        {
            return;
        }

        var orderedMaterial = await workOrderRepository.GetOrderedMaterialByIdAsync(request.OrderedMaterialId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("OrderedMaterial was not found.");
        if (orderedMaterial.WorkOrderId != report.WorkOrderId)
        {
            throw new ValidationException("OrderedMaterial must belong to the selected WorkOrder.");
        }

        if (orderedMaterial.MaterialId != request.MaterialId)
        {
            throw new ValidationException("MaterialUsage Material must match OrderedMaterial Material.");
        }
    }

    private async Task RecordStatusChangeAsync(
        Guid reportId,
        DailyReportStatus fromStatus,
        DailyReportStatus toStatus,
        string? reason,
        CancellationToken cancellationToken)
    {
        var history = new DailyReportStatusHistory
        {
            DailyReportId = reportId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Reason = reason,
            ChangedById = currentUserContext.UserId ?? Guid.Empty,
            ChangedAt = DateTime.UtcNow,
        };
        await dailyReportRepository.AddStatusHistoryAsync(history, cancellationToken);
    }
}

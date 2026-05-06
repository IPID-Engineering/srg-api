using SRG.Domain.Enums;

namespace SRG.Application.DailyReports;

public record CreateDailyReportRequest(DateOnly Date, Guid CrewId, Guid SectionId, Guid? WorkOrderId, string? Notes);

public record DailyReportResponse(
    Guid Id,
    DateOnly Date,
    Guid? CrewId,
    string? CrewName,
    Guid? SubcontractorCrewId,
    string? SubcontractorCrewName,
    Guid? ProjectId,
    Guid? SectionId,
    Guid? WorkOrderId,
    Guid? CreatedById,
    DailyReportStatus Status,
    string? Notes,
    string? RejectionReason,
    DateTime CreatedAt,
    bool CanEdit,
    string? EditLockReason,
    bool HasUnresolvedComments,
    List<WorkHourResponse> WorkHours,
    List<WorkEntryResponse> WorkEntries,
    List<MaterialUsageResponse> MaterialUsages,
    List<DailyReportCommentResponse> Comments,
    List<DailyReportStatusHistoryResponse> StatusHistory);

public record DailyReportCalendarResponse(
    Guid Id,
    DateOnly Date,
    DailyReportStatus Status,
    bool HasWorkHours,
    bool HasWorkEntries,
    bool HasMaterialUsages,
    bool HasUnresolvedComments,
    int UnresolvedCommentCount);

public record AddWorkHourRequest(Guid? WorkerId, Guid? SubcontractorWorkerId, decimal Hours);

public record WorkHourResponse(Guid Id, Guid DailyReportId, Guid? WorkerId, Guid? SubcontractorWorkerId, string? WorkerName, decimal Hours);

public record AddWorkEntryRequest(Guid WorkTypeId, Guid? OrderedWorkId, string? Description, decimal Quantity);

public record WorkEntryResponse(Guid Id, Guid DailyReportId, Guid WorkTypeId, Guid? OrderedWorkId, string? WorkTypeName, string? Unit, string? Description, decimal Quantity);

public record AddMaterialUsageRequest(Guid MaterialId, Guid? OrderedMaterialId, decimal Quantity);

public record MaterialUsageResponse(Guid Id, Guid DailyReportId, Guid MaterialId, Guid? OrderedMaterialId, string? MaterialName, string? Unit, decimal Quantity);

public record UpdateDailyReportNotesRequest(string? Notes);

public record UpdateDailyReportWorkOrderRequest(Guid? WorkOrderId);

public record RejectDailyReportRequest(string Reason);

public record AddDailyReportCommentRequest(DailyReportCommentSection Section, Guid? RecordId, string Content, Guid? ParentCommentId);

public record DailyReportCommentResponse(
    Guid Id,
    DailyReportCommentSection Section,
    Guid? RecordId,
    Guid? AuthorId,
    Guid? SubcontractorWorkerId,
    string AuthorEmail,
    string AuthorRole,
    string Content,
    Guid? ParentCommentId,
    bool IsResolved,
    DateTime CreatedAt,
    List<DailyReportCommentResponse> Replies);

public record ResolveDailyReportCommentRequest(Guid CommentId);

public record DailyReportStatusHistoryResponse(
    Guid Id,
    DailyReportStatus FromStatus,
    DailyReportStatus ToStatus,
    string? Reason,
    Guid ChangedById,
    string? ChangedByEmail,
    DateTime ChangedAt);

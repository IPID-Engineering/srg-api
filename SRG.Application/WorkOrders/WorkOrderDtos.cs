using SRG.Domain.Enums;

namespace SRG.Application.WorkOrders;

public record WorkTypeRequest(string Code, string Name, string? Description, string Unit, bool IsActive);

public record WorkTypeResponse(Guid Id, string Code, string Name, string? Description, string Unit, bool IsActive, DateTime CreatedAt);

public record CreateWorkOrderRequest(
    Guid ProjectId,
    Guid? SectionId,
    Guid? CrewId,
    Guid? SubcontractorCrewId,
    Guid? SubcontractorId,
    string? Description,
    string? DocumentationUrl,
    DateOnly? PlannedEndDate);

public record UpdateWorkOrderRequest(
    WorkOrderStatus Status,
    Guid? SectionId,
    Guid? CrewId,
    Guid? SubcontractorCrewId,
    Guid? SubcontractorId,
    string? Description,
    string? DocumentationUrl,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate);

public record AddOrderedWorkRequest(Guid WorkTypeId, Guid? SectionId, Guid? InstallationId, string? Description, decimal PlannedQuantity, string Unit);

public record AddOrderedMaterialRequest(Guid MaterialId, decimal PlannedQuantity, string Unit);

public record WorkOrderResponse(
    Guid Id,
    string Number,
    Guid ProjectId,
    string? ProjectName,
    Guid? SectionId,
    string? SectionName,
    Guid? CrewId,
    string? CrewName,
    Guid? SubcontractorCrewId,
    string? SubcontractorCrewName,
    string? ForemanName,
    Guid? SubcontractorId,
    string? SubcontractorName,
    Guid CreatedById,
    string? CreatedByName,
    WorkOrderStatus Status,
    string? Description,
    string? DocumentationUrl,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateTime CreatedAt,
    List<OrderedWorkResponse> OrderedWorks,
    List<OrderedMaterialResponse> OrderedMaterials);

public record OrderedWorkResponse(
    Guid Id,
    Guid WorkOrderId,
    Guid WorkTypeId,
    string? WorkTypeCode,
    string? WorkTypeName,
    Guid? SectionId,
    string? SectionName,
    Guid? InstallationId,
    string? InstallationName,
    string? Description,
    decimal PlannedQuantity,
    string Unit,
    DateTime CreatedAt);

public record OrderedMaterialResponse(
    Guid Id,
    Guid WorkOrderId,
    Guid MaterialId,
    string? MaterialName,
    decimal PlannedQuantity,
    string Unit,
    Guid? AddedById,
    string? AddedByName,
    string? AddedByRole,
    DateTime CreatedAt);

public record WorkOrderProgressResponse(
    Guid WorkOrderId,
    decimal PlannedWorkQuantity,
    decimal ReportedWorkQuantity,
    decimal PlannedMaterialQuantity,
    decimal UsedMaterialQuantity,
    decimal ProgressPercentage);

public record DeleteWorkOrderImpactResponse(
    int DailyReportsCount,
    int IssuesCount,
    int MaterialRequestsCount,
    int OrderedWorksCount,
    int OrderedMaterialsCount,
    bool HasConfirmedIssues,
    bool CanDelete);

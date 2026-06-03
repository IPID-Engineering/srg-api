using SRG.Domain.Enums;

namespace SRG.Application.Warehouses;

public record CreateMaterialRequest(string Name, Guid CategoryId, string Unit);

public record MaterialResponse(Guid Id, string Name, Guid CategoryId, string CategoryName, string Unit, DateTime CreatedAt);

public record CreateCategoryRequest(string Name, Guid? ParentCategoryId);

public record UpdateCategoryRequest(string? Name, Guid? ParentCategoryId);

public record CategoryResponse(
    Guid Id,
    string Name,
    string? FamilyCode,
    string? SubFamilyCode,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    int SubCategoryCount,
    int MaterialCount,
    decimal TotalUsage,
    DateTime CreatedAt);

public record CategoryTreeResponse(
    Guid Id,
    string Name,
    string? FamilyCode,
    string? SubFamilyCode,
    Guid? ParentCategoryId,
    int MaterialCount,
    List<CategoryTreeResponse> SubCategories);

public record ImportCategoryItem(
    string FamilyCode,
    string FamilyNameEN,
    string? FamilyNamePL,
    string? SubFamilyCode,
    string? SubFamilyNameEN,
    string? SubFamilyNamePL);

public record ImportCategoriesRequest(List<ImportCategoryItem> Items);

public record ImportCategoriesResult(int FamiliesCreated, int FamiliesUpdated, int SubFamiliesCreated, int SubFamiliesUpdated);

public record WarehouseResponse(Guid Id, string Name, WarehouseType Type, Guid? OwnerId);

public record StockResponse(
    Guid Id, 
    Guid WarehouseId, 
    Guid MaterialId, 
    string MaterialName, 
    string Unit, 
    decimal Quantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    Guid? CategoryId,
    string? CategoryName,
    Guid? ParentCategoryId,
    string? ParentCategoryName);

public record CreateIssueRequest(Guid WorkOrderId);

public record AddIssueItemRequest(Guid MaterialId, decimal Quantity);

public record ConfirmIssueRequest(
    Guid? ReceivedByWorkerId = null,
    Guid? ReceivedBySubcontractorWorkerId = null);

public record IssueResponse(
    Guid Id,
    string Number,
    Guid WorkOrderId,
    string WorkOrderNumber,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    string? ToWarehouseName,
    Guid CreatedById,
    string? CreatedByName,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    IssueStatus Status,
    string? ReceivedByName,
    string? VerificationCode,
    List<IssueItemResponse> Items);

public record IssueItemResponse(Guid Id, Guid IssueId, Guid MaterialId, string? MaterialName, string? Unit, decimal Quantity);

public record IssueWorkerOption(Guid Id, string FullName, bool IsSubcontractor);

public record IssueVerificationResponse(
    bool IsValid,
    string? IssueNumber,
    string? ProjectName,
    string? CrewName,
    string? IssuedByName,
    string? ReceivedByName,
    DateTime? ConfirmedAt,
    int ItemCount,
    string? Message);

public record CreateReturnRequest;

public record AddReturnItemRequest(Guid MaterialId, decimal Quantity);

public record ReturnResponse(
    Guid Id,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    Guid CreatedById,
    DateTime CreatedAt,
    ReturnStatus Status,
    List<ReturnItemResponse> Items);

public record ReturnItemResponse(Guid Id, Guid ReturnId, Guid MaterialId, string? MaterialName, decimal Quantity);

public record CreateGoodsReceivedVoucherRequest(string Number, string? SupplierName, DateOnly DeliveryDate);

public record AddGoodsReceivedVoucherItemRequest(Guid MaterialId, int? LineNumber, string? PartNumber, string? VendorPartNumber, decimal Quantity, string Unit, decimal? UnitPrice, decimal? ExtendedPrice, string? Status);

public record ImportGrvRequest(
    string Number,
    string? SupplierName,
    DateOnly DeliveryDate,
    List<ImportGrvItemRequest> Items);

public record ImportGrvItemRequest(
    int? LineNumber,
    string? PartNumber,
    string? VendorPartNumber,
    string MaterialName,
    decimal Quantity,
    string Unit,
    decimal? UnitPrice,
    decimal? ExtendedPrice,
    string? Status);

public record GoodsReceivedVoucherResponse(
    Guid Id,
    string Number,
    Guid WarehouseId,
    Guid CreatedById,
    string? SupplierName,
    DateOnly DeliveryDate,
    GoodsReceivedVoucherStatus Status,
    DateTime CreatedAt,
    List<GoodsReceivedVoucherItemResponse> Items);

public record GoodsReceivedVoucherItemResponse(
    Guid Id,
    Guid GoodsReceivedVoucherId,
    Guid MaterialId,
    string? MaterialName,
    int? LineNumber,
    string? PartNumber,
    string? VendorPartNumber,
    decimal Quantity,
    string Unit,
    decimal? UnitPrice,
    decimal? ExtendedPrice,
    string? Status);

public record StockMovementResponse(
    Guid Id,
    Guid WarehouseId,
    Guid MaterialId,
    string? MaterialName,
    string? CategoryName,
    decimal Quantity,
    decimal QuantityBefore,
    decimal QuantityAfter,
    StockMovementDirection Direction,
    StockMovementSourceType SourceType,
    Guid SourceId,
    string? SourceNumber,
    string? WorkOrderNumber,
    string? TargetWarehouseName,
    Guid CreatedById,
    string? CreatedByName,
    DateTime CreatedAt);

public record CheckMaterialAvailabilityRequest(
    decimal Quantity,
    Guid? ExcludeWorkOrderId = null,
    int DaysAhead = 14);

public record MaterialConflictInfo(
    Guid WorkOrderId,
    string WorkOrderNumber,
    string? ProjectName,
    string? CrewName,
    DateOnly? PlannedEndDate,
    decimal PlannedQuantity,
    decimal IssuedQuantity,
    decimal RemainingNeeded,
    decimal ShortageIfProceeded);

public record MaterialAvailabilityResponse(
    Guid MaterialId,
    string MaterialName,
    string Unit,
    decimal CurrentStock,
    decimal ReservedQuantity,
    decimal AvailableStock,
    decimal RequestedQuantity,
    decimal TotalPlannedInOtherOrders,
    decimal AfterAllocationAvailable,
    bool HasConflict,
    string? ConflictSeverity,
    List<MaterialConflictInfo> Conflicts);

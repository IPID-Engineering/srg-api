using SRG.Domain.Entities;

namespace SRG.Application.MaterialRequests;

public record MaterialRequestResponse(
    Guid Id,
    Guid WorkOrderId,
    string WorkOrderNumber,
    Guid MaterialId,
    string MaterialName,
    string Unit,
    decimal Quantity,
    string? Notes,
    string Status,
    Guid CreatedById,
    string? CreatedByName,
    DateTime CreatedAt,
    Guid? ProcessedById,
    DateTime? ProcessedAt,
    string? ProcessingNotes);

public record CreateMaterialRequestRequest(
    Guid MaterialId,
    decimal Quantity,
    string? Notes);

public record ProcessMaterialRequestRequest(
    bool Approved,
    string? Notes);

public static class MaterialRequestMapper
{
    public static MaterialRequestResponse ToResponse(MaterialRequest request)
    {
        var createdByName = request.CreatedByWorker != null 
            ? $"{request.CreatedByWorker.FirstName} {request.CreatedByWorker.LastName}"
            : null;
            
        return new MaterialRequestResponse(
            request.Id,
            request.WorkOrderId,
            request.WorkOrder?.Number ?? "",
            request.MaterialId,
            request.Material?.Name ?? "",
            request.Material?.Unit ?? "szt.",
            request.Quantity,
            request.Notes,
            request.Status.ToString(),
            request.CreatedById,
            createdByName,
            request.CreatedAt,
            request.ProcessedById,
            request.ProcessedAt,
            request.ProcessingNotes);
    }
}

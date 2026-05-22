using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.MaterialRequests;

public interface IMaterialRequestService
{
    Task<MaterialRequestResponse> CreateRequestAsync(Guid workOrderId, Guid createdById, CreateMaterialRequestRequest request, CancellationToken cancellationToken = default);
    Task<List<MaterialRequestResponse>> GetByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<List<MaterialRequestResponse>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);
    Task<MaterialRequestResponse> ProcessRequestAsync(Guid requestId, Guid processedById, ProcessMaterialRequestRequest request, CancellationToken cancellationToken = default);
    Task DeleteRequestAsync(Guid requestId, Guid userId, CancellationToken cancellationToken = default);
}

public class MaterialRequestService(IWarehouseRepository warehouse) : IMaterialRequestService
{
    public async Task<MaterialRequestResponse> CreateRequestAsync(
        Guid workOrderId, 
        Guid createdById, 
        CreateMaterialRequestRequest request, 
        CancellationToken cancellationToken = default)
    {
        var materialRequest = new MaterialRequest
        {
            WorkOrderId = workOrderId,
            MaterialId = request.MaterialId,
            Quantity = request.Quantity,
            Notes = request.Notes,
            CreatedById = createdById,
            Status = MaterialRequestStatus.Pending
        };

        await warehouse.AddMaterialRequestAsync(materialRequest, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);

        var saved = await warehouse.GetMaterialRequestByIdAsync(materialRequest.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found");
        return MaterialRequestMapper.ToResponse(saved);
    }

    public async Task<List<MaterialRequestResponse>> GetByWorkOrderAsync(
        Guid workOrderId, 
        CancellationToken cancellationToken = default)
    {
        var requests = await warehouse.GetMaterialRequestsByWorkOrderAsync(workOrderId, cancellationToken);
        return requests.Select(MaterialRequestMapper.ToResponse).ToList();
    }

    public async Task<List<MaterialRequestResponse>> GetPendingRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        var requests = await warehouse.GetPendingMaterialRequestsAsync(cancellationToken);
        return requests.Select(MaterialRequestMapper.ToResponse).ToList();
    }

    public async Task<MaterialRequestResponse> ProcessRequestAsync(
        Guid requestId, 
        Guid processedById, 
        ProcessMaterialRequestRequest request, 
        CancellationToken cancellationToken = default)
    {
        var materialRequest = await warehouse.GetMaterialRequestByIdAsync(requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found");

        materialRequest.Status = request.Approved 
            ? MaterialRequestStatus.Approved 
            : MaterialRequestStatus.Rejected;
        materialRequest.ProcessedById = processedById;
        materialRequest.ProcessedAt = DateTime.UtcNow;
        materialRequest.ProcessingNotes = request.Notes;

        await warehouse.SaveChangesAsync(cancellationToken);

        return MaterialRequestMapper.ToResponse(materialRequest);
    }

    public async Task DeleteRequestAsync(Guid requestId, Guid userId, CancellationToken cancellationToken = default)
    {
        var request = await warehouse.GetMaterialRequestByIdAsync(requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Wniosek nie został znaleziony.");

        if (request.CreatedById != userId)
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException("Możesz usuwać tylko swoje wnioski.");
        }
        
        if (request.Status != MaterialRequestStatus.Pending)
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException("Można usuwać tylko wnioski oczekujące na rozpatrzenie.");
        }

        warehouse.RemoveMaterialRequest(request);
        await warehouse.SaveChangesAsync(cancellationToken);
    }
}

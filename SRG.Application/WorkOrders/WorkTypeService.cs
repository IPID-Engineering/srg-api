using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.WorkOrders;

public class WorkTypeService(IWorkOrderRepository workOrderRepository) : IWorkTypeService
{
    public async Task<List<WorkTypeResponse>> GetWorkTypesAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var workTypes = await workOrderRepository.GetWorkTypesAsync(cancellationToken);
        if (activeOnly)
        {
            workTypes = workTypes.Where(wt => wt.IsActive).ToList();
        }

        return workTypes.Select(ToResponse).ToList();
    }

    public async Task<WorkTypeResponse> CreateWorkTypeAsync(WorkTypeRequest request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.Code);
        if (await workOrderRepository.GetWorkTypeByCodeAsync(code, cancellationToken) is not null)
        {
            throw new ValidationException("WorkType code must be unique.");
        }

        var workType = new WorkType
        {
            Code = code,
            Name = RequiredText(request.Name, "Name"),
            Description = OptionalText(request.Description),
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? "szt" : request.Unit.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        await workOrderRepository.AddWorkTypeAsync(workType, cancellationToken);
        await workOrderRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(workType);
    }

    public async Task<WorkTypeResponse> UpdateWorkTypeAsync(Guid id, WorkTypeRequest request, CancellationToken cancellationToken = default)
    {
        var workType = await workOrderRepository.GetWorkTypeByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("WorkType was not found.");
        var code = NormalizeCode(request.Code);
        var existing = await workOrderRepository.GetWorkTypeByCodeAsync(code, cancellationToken);
        if (existing is not null && existing.Id != id)
        {
            throw new ValidationException("WorkType code must be unique.");
        }

        workType.Code = code;
        workType.Name = RequiredText(request.Name, "Name");
        workType.Description = OptionalText(request.Description);
        workType.Unit = string.IsNullOrWhiteSpace(request.Unit) ? "szt" : request.Unit.Trim();
        workType.IsActive = request.IsActive;
        await workOrderRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(workType);
    }

    public async Task<WorkTypeResponse> DeactivateWorkTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workType = await workOrderRepository.GetWorkTypeByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("WorkType was not found.");
        workType.IsActive = false;
        await workOrderRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(workType);
    }

    private static WorkTypeResponse ToResponse(WorkType workType)
    {
        return new WorkTypeResponse(workType.Id, workType.Code, workType.Name, workType.Description, workType.Unit, workType.IsActive, workType.CreatedAt);
    }

    private static string NormalizeCode(string value)
    {
        return RequiredText(value, "Code").ToUpperInvariant();
    }

    private static string RequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? OptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

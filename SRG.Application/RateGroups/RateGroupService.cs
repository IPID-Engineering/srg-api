using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.RateGroups;

public interface IRateGroupService
{
    Task<List<RateGroupResponse>> GetAllAsync(Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<RateGroupResponse> GetByIdAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task<RateGroupResponse> CreateAsync(Guid subcontractorId, CreateRateGroupRequest request, CancellationToken cancellationToken = default);
    Task<RateGroupResponse> UpdateAsync(Guid id, Guid subcontractorId, UpdateRateGroupRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default);
    Task AssignWorkerAsync(Guid workerId, Guid? rateGroupId, Guid subcontractorId, CancellationToken cancellationToken = default);
}

public record RateGroupResponse(
    Guid Id,
    string Name,
    decimal HourlyRate,
    decimal HourlyCost,
    int WorkerCount,
    DateTime CreatedAt);

public record CreateRateGroupRequest(
    string Name,
    decimal HourlyRate,
    decimal HourlyCost);

public record UpdateRateGroupRequest(
    string? Name,
    decimal? HourlyRate,
    decimal? HourlyCost);

public class RateGroupService(IRateGroupRepository repository, IConstructionRepository construction) : IRateGroupService
{
    public async Task<List<RateGroupResponse>> GetAllAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var groups = await repository.GetAllBySubcontractorAsync(subcontractorId, cancellationToken);
        return groups.Select(ToResponse).ToList();
    }

    public async Task<RateGroupResponse> GetByIdAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var group = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Grupa stawek nie została znaleziona.");
            
        if (group.SubcontractorId != subcontractorId)
            throw new UnauthorizedAccessException("Brak dostępu do tej grupy stawek.");
            
        return ToResponse(group);
    }

    public async Task<RateGroupResponse> CreateAsync(Guid subcontractorId, CreateRateGroupRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Nazwa grupy jest wymagana.");
            
        if (request.HourlyRate < 0)
            throw new ValidationException("Stawka godzinowa nie może być ujemna.");
            
        if (request.HourlyCost < 0)
            throw new ValidationException("Koszt godziny nie może być ujemny.");

        var group = new RateGroup
        {
            Name = request.Name.Trim(),
            HourlyRate = request.HourlyRate,
            HourlyCost = request.HourlyCost,
            SubcontractorId = subcontractorId
        };

        await repository.AddAsync(group, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return ToResponse(group);
    }

    public async Task<RateGroupResponse> UpdateAsync(Guid id, Guid subcontractorId, UpdateRateGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Grupa stawek nie została znaleziona.");
            
        if (group.SubcontractorId != subcontractorId)
            throw new UnauthorizedAccessException("Brak dostępu do tej grupy stawek.");

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Nazwa grupy nie może być pusta.");
            group.Name = request.Name.Trim();
        }

        if (request.HourlyRate.HasValue)
        {
            if (request.HourlyRate < 0)
                throw new ValidationException("Stawka godzinowa nie może być ujemna.");
            group.HourlyRate = request.HourlyRate.Value;
        }

        if (request.HourlyCost.HasValue)
        {
            if (request.HourlyCost < 0)
                throw new ValidationException("Koszt godziny nie może być ujemny.");
            group.HourlyCost = request.HourlyCost.Value;
        }

        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(group);
    }

    public async Task DeleteAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var group = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Grupa stawek nie została znaleziona.");
            
        if (group.SubcontractorId != subcontractorId)
            throw new UnauthorizedAccessException("Brak dostępu do tej grupy stawek.");

        await repository.DeleteAsync(group, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignWorkerAsync(Guid workerId, Guid? rateGroupId, Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var worker = await construction.GetSubcontractorWorkerByIdAsync(workerId, cancellationToken)
            ?? throw new KeyNotFoundException("Pracownik nie został znaleziony.");
            
        if (worker.SubcontractorId != subcontractorId)
            throw new UnauthorizedAccessException("Brak dostępu do tego pracownika.");

        if (rateGroupId.HasValue)
        {
            var group = await repository.GetByIdAsync(rateGroupId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Grupa stawek nie została znaleziona.");
                
            if (group.SubcontractorId != subcontractorId)
                throw new UnauthorizedAccessException("Brak dostępu do tej grupy stawek.");
        }

        worker.RateGroupId = rateGroupId;
        await construction.SaveChangesAsync(cancellationToken);
    }

    private static RateGroupResponse ToResponse(RateGroup group) => new(
        group.Id,
        group.Name,
        group.HourlyRate,
        group.HourlyCost,
        group.Workers.Count,
        group.CreatedAt);
}

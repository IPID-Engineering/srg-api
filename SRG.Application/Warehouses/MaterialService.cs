using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Warehouses;

public class MaterialService(IWarehouseRepository warehouse) : IMaterialService
{
    public async Task<MaterialResponse> CreateMaterialAsync(
        CreateMaterialRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Unit))
        {
            throw new ValidationException("Name and unit are required.");
        }

        var category = await warehouse.GetCategoryByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new ValidationException("Category not found.");

        var material = new Material
        {
            Name = request.Name.Trim(),
            CategoryId = category.Id,
            Unit = request.Unit.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        await warehouse.AddMaterialAsync(material, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);

        material.Category = category;
        return ToResponse(material);
    }

    public async Task<List<MaterialResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var materialUsages = await warehouse.GetMaterialsAsync(cancellationToken);
        return materialUsages.Select(ToResponse).ToList();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var material = await warehouse.GetMaterialByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Materiał nie został znaleziony.");

        var isInUse = await warehouse.IsMaterialInUseAsync(id, cancellationToken);
        if (isInUse)
        {
            throw new ValidationException("Nie można usunąć materiału, który jest używany w zleceniach, magazynie lub innych dokumentach.");
        }

        warehouse.RemoveMaterial(material);
        await warehouse.SaveChangesAsync(cancellationToken);
    }

    private static MaterialResponse ToResponse(Material material)
    {
        return new MaterialResponse(
            material.Id,
            material.Name,
            material.CategoryId,
            material.Category?.Name ?? string.Empty,
            material.Unit,
            material.CreatedAt);
    }
}

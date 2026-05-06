using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Warehouses;

public class CategoryService(IWarehouseRepository warehouse) : ICategoryService
{
    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Category name is required.");
        }

        var existing = await warehouse.GetCategoryByNameAsync(request.Name.Trim(), cancellationToken);
        if (existing != null)
        {
            throw new ValidationException("Category with this name already exists.");
        }

        Category? parentCategory = null;
        if (request.ParentCategoryId.HasValue)
        {
            parentCategory = await warehouse.GetCategoryByIdAsync(request.ParentCategoryId.Value, cancellationToken)
                ?? throw new ValidationException("Parent category not found.");
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            ParentCategoryId = request.ParentCategoryId,
            CreatedAt = DateTime.UtcNow,
        };

        await warehouse.AddCategoryAsync(category, cancellationToken);
        await warehouse.SaveChangesAsync(cancellationToken);

        category.ParentCategory = parentCategory;
        return ToResponse(category, new Dictionary<Guid, decimal>());
    }

    public async Task<List<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await warehouse.GetCategoriesAsync(cancellationToken);
        var usageTotals = await warehouse.GetCategoryUsageTotalsAsync(cancellationToken);
        return categories.Select(c => ToResponse(c, usageTotals)).ToList();
    }

    public async Task<List<CategoryTreeResponse>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var categories = await warehouse.GetCategoriesAsync(cancellationToken);
        var rootCategories = categories.Where(c => c.ParentCategoryId == null).ToList();
        return rootCategories.Select(c => ToTreeResponse(c, categories)).ToList();
    }

    public async Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await warehouse.GetCategoryByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Category not found.");

        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name.Trim() != category.Name)
        {
            var existing = await warehouse.GetCategoryByNameAsync(request.Name.Trim(), cancellationToken);
            if (existing != null && existing.Id != id)
            {
                throw new ValidationException("Category with this name already exists.");
            }
            category.Name = request.Name.Trim();
        }

        if (request.ParentCategoryId != category.ParentCategoryId)
        {
            if (request.ParentCategoryId.HasValue)
            {
                if (request.ParentCategoryId.Value == id)
                {
                    throw new ValidationException("Category cannot be its own parent.");
                }

                var parent = await warehouse.GetCategoryByIdAsync(request.ParentCategoryId.Value, cancellationToken)
                    ?? throw new ValidationException("Parent category not found.");

                category.ParentCategoryId = request.ParentCategoryId;
                category.ParentCategory = parent;
            }
            else
            {
                category.ParentCategoryId = null;
                category.ParentCategory = null;
            }
        }

        await warehouse.SaveChangesAsync(cancellationToken);
        var usageTotals = await warehouse.GetCategoryUsageTotalsAsync(cancellationToken);
        return ToResponse(category, usageTotals);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await warehouse.GetCategoryByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Category not found.");

        if (category.Materials?.Count > 0)
        {
            throw new ValidationException("Cannot delete category with materials. Move materials first.");
        }

        if (category.SubCategories?.Count > 0)
        {
            throw new ValidationException("Cannot delete category with subcategories. Delete subcategories first.");
        }

        warehouse.RemoveCategory(category);
        await warehouse.SaveChangesAsync(cancellationToken);
    }

    public async Task<ImportCategoriesResult> ImportCategoriesAsync(ImportCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        var familiesCreated = 0;
        var familiesUpdated = 0;
        var subFamiliesCreated = 0;
        var subFamiliesUpdated = 0;

        var familyGroups = request.Items
            .GroupBy(item => item.FamilyCode)
            .ToList();

        foreach (var familyGroup in familyGroups)
        {
            var firstItem = familyGroup.First();
            var familyCode = firstItem.FamilyCode;
            var familyName = firstItem.FamilyNamePL ?? firstItem.FamilyNameEN;

            var familyCategory = await warehouse.GetCategoryByCodesAsync(familyCode, null, cancellationToken);
            if (familyCategory == null)
            {
                familyCategory = new Category
                {
                    Name = familyName,
                    FamilyCode = familyCode,
                    SubFamilyCode = null,
                    ParentCategoryId = null,
                    CreatedAt = DateTime.UtcNow
                };
                await warehouse.AddCategoryAsync(familyCategory, cancellationToken);
                await warehouse.SaveChangesAsync(cancellationToken);
                familiesCreated++;
            }
            else if (familyCategory.Name != familyName)
            {
                familyCategory.Name = familyName;
                await warehouse.SaveChangesAsync(cancellationToken);
                familiesUpdated++;
            }

            foreach (var item in familyGroup.Where(i => !string.IsNullOrWhiteSpace(i.SubFamilyCode)))
            {
                var subFamilyCode = item.SubFamilyCode!;
                var subFamilyName = item.SubFamilyNamePL ?? item.SubFamilyNameEN ?? "Unknown";

                var subFamilyCategory = await warehouse.GetCategoryByCodesAsync(familyCode, subFamilyCode, cancellationToken);
                if (subFamilyCategory == null)
                {
                    subFamilyCategory = new Category
                    {
                        Name = subFamilyName,
                        FamilyCode = familyCode,
                        SubFamilyCode = subFamilyCode,
                        ParentCategoryId = familyCategory.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    await warehouse.AddCategoryAsync(subFamilyCategory, cancellationToken);
                    subFamiliesCreated++;
                }
                else if (subFamilyCategory.Name != subFamilyName || subFamilyCategory.ParentCategoryId != familyCategory.Id)
                {
                    subFamilyCategory.Name = subFamilyName;
                    subFamilyCategory.ParentCategoryId = familyCategory.Id;
                    subFamiliesUpdated++;
                }
            }
            await warehouse.SaveChangesAsync(cancellationToken);
        }

        return new ImportCategoriesResult(familiesCreated, familiesUpdated, subFamiliesCreated, subFamiliesUpdated);
    }

    private static CategoryResponse ToResponse(Category category, Dictionary<Guid, decimal> usageTotals)
    {
        var directUsage = usageTotals.GetValueOrDefault(category.Id, 0);
        var subCategoryUsage = category.SubCategories?
            .Sum(sub => usageTotals.GetValueOrDefault(sub.Id, 0)) ?? 0;
        
        return new CategoryResponse(
            category.Id,
            category.Name,
            category.FamilyCode,
            category.SubFamilyCode,
            category.ParentCategoryId,
            category.ParentCategory?.Name,
            category.SubCategories?.Count ?? 0,
            category.Materials?.Count ?? 0,
            directUsage + subCategoryUsage,
            category.CreatedAt);
    }

    private static CategoryTreeResponse ToTreeResponse(Category category, List<Category> allCategories)
    {
        var subCategories = allCategories.Where(c => c.ParentCategoryId == category.Id).ToList();
        return new CategoryTreeResponse(
            category.Id,
            category.Name,
            category.FamilyCode,
            category.SubFamilyCode,
            category.ParentCategoryId,
            category.Materials?.Count ?? 0,
            subCategories.Select(c => ToTreeResponse(c, allCategories)).ToList());
    }
}

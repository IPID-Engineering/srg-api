namespace SRG.Application.Warehouses;

public interface ICategoryService
{
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<List<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<CategoryTreeResponse>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ImportCategoriesResult> ImportCategoriesAsync(ImportCategoriesRequest request, CancellationToken cancellationToken = default);
}

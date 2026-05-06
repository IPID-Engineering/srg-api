namespace SRG.Application.Warehouses;

public interface IMaterialService
{
    Task<MaterialResponse> CreateMaterialAsync(CreateMaterialRequest request, CancellationToken cancellationToken = default);
    Task<List<MaterialResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

namespace SRG.Domain.Entities;

public class Material
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public required string Unit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<WarehouseStock> WarehouseStocks { get; set; } = [];
}

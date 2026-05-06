using SRG.Domain.Enums;

namespace SRG.Domain.Entities;

public class Warehouse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public WarehouseType Type { get; set; }
    public Guid? OwnerId { get; set; }
    public ICollection<WarehouseStock> WarehouseStocks { get; set; } = [];
}

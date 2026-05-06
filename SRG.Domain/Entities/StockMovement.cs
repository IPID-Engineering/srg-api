using SRG.Domain.Enums;

namespace SRG.Domain.Entities;

public class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid MaterialId { get; set; }
    public Material? Material { get; set; }
    public decimal Quantity { get; set; }
    public StockMovementDirection Direction { get; set; }
    public StockMovementSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

namespace SRG.Domain.Entities;

public class WarehouseStock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid MaterialId { get; set; }
    public Material? Material { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    
    public decimal AvailableQuantity => Quantity - ReservedQuantity;
}

namespace SRG.Domain.Entities;

public class OrderedMaterial
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public Guid MaterialId { get; set; }
    public Material? Material { get; set; }
    public decimal PlannedQuantity { get; set; }
    public required string Unit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<MaterialUsage> MaterialUsages { get; set; } = [];
}

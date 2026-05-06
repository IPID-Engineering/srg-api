namespace SRG.Domain.Entities;

public enum MaterialRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Fulfilled
}

public class MaterialRequest
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public MaterialRequestStatus Status { get; set; } = MaterialRequestStatus.Pending;
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ProcessedById { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingNotes { get; set; }
    
    public WorkOrder? WorkOrder { get; set; }
    public Material? Material { get; set; }
}

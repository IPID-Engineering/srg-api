namespace SRG.Domain.Entities;

public class OrderedWork
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public Guid WorkTypeId { get; set; }
    public WorkType? WorkType { get; set; }
    public Guid? SectionId { get; set; }
    public Section? Section { get; set; }
    public Guid? InstallationId { get; set; }
    public Installation? Installation { get; set; }
    public string? Description { get; set; }
    public decimal PlannedQuantity { get; set; }
    public required string Unit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<WorkEntry> WorkEntries { get; set; } = [];
}

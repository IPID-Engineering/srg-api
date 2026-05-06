namespace SRG.Domain.Entities;

public class MaterialUsage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DailyReportId { get; set; }
    public DailyReport? DailyReport { get; set; }
    public Guid MaterialId { get; set; }
    public Material? Material { get; set; }
    public Guid? OrderedMaterialId { get; set; }
    public OrderedMaterial? OrderedMaterial { get; set; }
    public decimal Quantity { get; set; }
}

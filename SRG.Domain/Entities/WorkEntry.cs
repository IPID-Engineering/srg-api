namespace SRG.Domain.Entities;

public class WorkEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DailyReportId { get; set; }
    public DailyReport? DailyReport { get; set; }
    public Guid WorkTypeId { get; set; }
    public WorkType? WorkType { get; set; }
    public Guid? OrderedWorkId { get; set; }
    public OrderedWork? OrderedWork { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
}

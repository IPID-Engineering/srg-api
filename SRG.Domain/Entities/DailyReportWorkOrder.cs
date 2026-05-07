namespace SRG.Domain.Entities;

public class DailyReportWorkOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DailyReportId { get; set; }
    public DailyReport? DailyReport { get; set; }
    public Guid WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

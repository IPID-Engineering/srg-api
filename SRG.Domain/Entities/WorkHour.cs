namespace SRG.Domain.Entities;

public class WorkHour
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DailyReportId { get; set; }
    public DailyReport? DailyReport { get; set; }
    public Guid? WorkerId { get; set; }
    public Worker? Worker { get; set; }
    public Guid? SubcontractorWorkerId { get; set; }
    public SubcontractorWorker? SubcontractorWorker { get; set; }
    public decimal Hours { get; set; }
    public bool IsAbsent { get; set; }
}

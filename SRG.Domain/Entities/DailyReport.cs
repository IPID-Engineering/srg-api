using SRG.Domain.Enums;

namespace SRG.Domain.Entities;

public class DailyReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    
    /// <summary>
    /// ID brygady (opcjonalne - używane dla zwykłych brygad Foreman).
    /// </summary>
    public Guid? CrewId { get; set; }
    public Crew? Crew { get; set; }
    
    /// <summary>
    /// ID brygady podwykonawcy (opcjonalne - używane dla SubcontractorForeman).
    /// </summary>
    public Guid? SubcontractorCrewId { get; set; }
    public SubcontractorCrew? SubcontractorCrew { get; set; }
    
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid? SectionId { get; set; }
    public Section? Section { get; set; }
    public Guid? WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public ICollection<DailyReportWorkOrder> DailyReportWorkOrders { get; set; } = [];
    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DailyReportStatus Status { get; set; } = DailyReportStatus.Draft;
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<WorkHour> WorkHours { get; set; } = [];
    public ICollection<WorkEntry> WorkEntries { get; set; } = [];
    public ICollection<MaterialUsage> MaterialUsages { get; set; } = [];
    public ICollection<DailyReportComment> Comments { get; set; } = [];
    public ICollection<DailyReportStatusHistory> StatusHistory { get; set; } = [];
}

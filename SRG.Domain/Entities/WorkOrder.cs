using SRG.Domain.Enums;

namespace SRG.Domain.Entities;

public class WorkOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Number { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid? SectionId { get; set; }
    public Section? Section { get; set; }
    public Guid? CrewId { get; set; }
    public Crew? Crew { get; set; }
    public Guid? SubcontractorCrewId { get; set; }
    public SubcontractorCrew? SubcontractorCrew { get; set; }
    public Guid? SubcontractorId { get; set; }
    public User? Subcontractor { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;
    public string? Description { get; set; }
    public string? DocumentationUrl { get; set; }
    public DateOnly? PlannedStartDate { get; set; }
    public DateOnly? PlannedEndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OrderedWork> OrderedWorks { get; set; } = [];
    public ICollection<OrderedMaterial> OrderedMaterials { get; set; } = [];
    public ICollection<DailyReport> DailyReports { get; set; } = [];
    public ICollection<DailyReportWorkOrder> DailyReportWorkOrders { get; set; } = [];
    public ICollection<Issue> Issues { get; set; } = [];
}

namespace SRG.Domain.Entities;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Section> Sections { get; set; } = [];
    public ICollection<Crew> Crews { get; set; } = [];
    public ICollection<DailyReport> DailyReports { get; set; } = [];
    public ICollection<ProjectSubcontractor> ProjectSubcontractors { get; set; } = [];
}

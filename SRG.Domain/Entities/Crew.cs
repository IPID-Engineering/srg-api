namespace SRG.Domain.Entities;

public class Crew
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Team> Teams { get; set; } = [];
    public ICollection<Worker> Worker { get; set; } = [];
    public ICollection<DailyReport> DailyReports { get; set; } = [];
    
    /// <summary>
    /// Lista użytkowników (PM i Subcontractor) z dostępem do tej brygady.
    /// Zarządzane przez Admina.
    /// </summary>
    public ICollection<CrewAccess> AccessList { get; set; } = [];
}

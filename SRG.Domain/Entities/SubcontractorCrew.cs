namespace SRG.Domain.Entities;

public class SubcontractorCrew
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public Guid SubcontractorId { get; set; }
    public User? Subcontractor { get; set; }
    public Guid? CurrentForemanId { get; set; }
    public SubcontractorWorker? CurrentForeman { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<SubcontractorWorker> Workers { get; set; } = [];
    public ICollection<SubcontractorForemanHistory> ForemanHistory { get; set; } = [];
    
    /// <summary>
    /// Lista PM-ów z dostępem do tej brygady.
    /// Zarządzane przez właściciela brygady (Subcontractor).
    /// </summary>
    public ICollection<SubcontractorCrewPmAccess> PmAccessList { get; set; } = [];
    
    /// <summary>
    /// Dzienne karty pracy tej brygady.
    /// </summary>
    public ICollection<DailyReport> DailyReports { get; set; } = [];
}

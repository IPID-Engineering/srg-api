namespace SRG.Domain.Entities;

public class DailyReportChangeHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DailyReportId { get; set; }
    public DailyReport? DailyReport { get; set; }
    
    /// <summary>
    /// Type of entry changed: WorkEntry, MaterialUsage, WorkHour
    /// </summary>
    public string EntryType { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the changed entry
    /// </summary>
    public Guid EntryId { get; set; }
    
    /// <summary>
    /// Type of change: Created, Updated, Deleted
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON containing old values (null for Created)
    /// </summary>
    public string? OldValues { get; set; }
    
    /// <summary>
    /// JSON containing new values (null for Deleted)
    /// </summary>
    public string? NewValues { get; set; }
    
    public Guid ChangedById { get; set; }
    public User? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

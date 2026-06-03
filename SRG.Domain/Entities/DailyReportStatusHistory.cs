using SRG.Domain.Enums;

namespace SRG.Domain.Entities;

public class DailyReportStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DailyReportId { get; set; }
    public DailyReport? DailyReport { get; set; }
    public DailyReportStatus FromStatus { get; set; }
    public DailyReportStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    
    public Guid? ChangedById { get; set; }
    public User? ChangedBy { get; set; }
    
    /// <summary>
    /// For changes made by foremen (not in Users table)
    /// </summary>
    public Guid? ChangedByWorkerId { get; set; }
    public SubcontractorWorker? ChangedByWorker { get; set; }
    public string? ChangedByEmail { get; set; }
    
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

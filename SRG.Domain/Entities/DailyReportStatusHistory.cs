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
    public Guid ChangedById { get; set; }
    public User? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

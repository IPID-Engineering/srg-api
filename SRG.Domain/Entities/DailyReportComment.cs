using SRG.Domain.Enums;

namespace SRG.Domain.Entities;

public class DailyReportComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DailyReportId { get; set; }
    public DailyReport? DailyReport { get; set; }
    public DailyReportCommentSection Section { get; set; }
    public Guid? RecordId { get; set; }
    
    // Author can be either a User or SubcontractorWorker
    public Guid? AuthorId { get; set; }
    public User? Author { get; set; }
    public Guid? SubcontractorWorkerId { get; set; }
    public SubcontractorWorker? SubcontractorWorker { get; set; }
    
    // Denormalized author info for display
    public string AuthorEmail { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public DailyReportComment? ParentComment { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<DailyReportComment> Replies { get; set; } = [];
}

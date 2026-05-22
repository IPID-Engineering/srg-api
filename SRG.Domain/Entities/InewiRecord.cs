namespace SRG.Domain.Entities;

public class InewiRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubcontractorCrewId { get; set; }
    public SubcontractorCrew? SubcontractorCrew { get; set; }
    public required string WorkerName { get; set; }
    public DateOnly Date { get; set; }
    public decimal Hours { get; set; }
    public string? SourceFileName { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public Guid ImportedById { get; set; }
    public User? ImportedBy { get; set; }
}

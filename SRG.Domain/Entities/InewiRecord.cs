namespace SRG.Domain.Entities;

/// <summary>
/// Rekord godzin pracy z systemu INEWI.
/// Globalny dla całego podwykonawcy, nie powiązany z konkretną brygadą.
/// </summary>
public class InewiRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubcontractorId { get; set; }
    public User? Subcontractor { get; set; }
    public required string WorkerName { get; set; }
    public DateOnly Date { get; set; }
    public decimal Hours { get; set; }
    public string? SourceFileName { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public Guid ImportedById { get; set; }
    public User? ImportedBy { get; set; }
}

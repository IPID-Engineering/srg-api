namespace SRG.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Action { get; set; }
    public required string EntityName { get; set; }
    public Guid EntityId { get; set; }
    public required string Changes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

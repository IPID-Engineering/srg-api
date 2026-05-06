namespace SRG.Domain.Entities;

public class Installation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid SectionId { get; set; }
    public Section? Section { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

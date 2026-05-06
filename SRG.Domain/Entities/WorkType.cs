namespace SRG.Domain.Entities;

public class WorkType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string Unit { get; set; } = "szt";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<WorkEntry> WorkEntries { get; set; } = [];
    public ICollection<OrderedWork> OrderedWorks { get; set; } = [];
}

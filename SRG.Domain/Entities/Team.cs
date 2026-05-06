namespace SRG.Domain.Entities;

public class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public Guid CrewId { get; set; }
    public Crew? Crew { get; set; }
    public ICollection<Worker> Worker { get; set; } = [];
}

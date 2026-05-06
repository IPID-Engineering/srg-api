namespace SRG.Domain.Entities;

public class Worker
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Guid CrewId { get; set; }
    public Crew? Crew { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }
    public ICollection<WorkHour> WorkHour { get; set; } = [];
}

namespace SRG.Domain.Entities;

public class ProjectSubcontractor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid SubcontractorId { get; set; }
    public User? Subcontractor { get; set; }
}

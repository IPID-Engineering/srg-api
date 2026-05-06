namespace SRG.Domain.Entities;

public class Section
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public ICollection<DailyReport> DailyReports { get; set; } = [];
    public ICollection<Installation> Installations { get; set; } = [];
}

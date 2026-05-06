namespace SRG.Domain.Entities;

public class SubcontractorForemanHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CrewId { get; set; }
    public SubcontractorCrew? Crew { get; set; }
    public Guid ForemanId { get; set; }
    public SubcontractorWorker? Foreman { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

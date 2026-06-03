namespace SRG.Domain.Entities;

public class RateGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    
    /// <summary>
    /// Stawka godzinowa - ile fakturuje podwykonawca
    /// </summary>
    public decimal HourlyRate { get; set; }
    
    /// <summary>
    /// Koszt godziny - ile płacimy za tę godzinę
    /// </summary>
    public decimal HourlyCost { get; set; }
    
    public Guid SubcontractorId { get; set; }
    public User? Subcontractor { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<SubcontractorWorker> Workers { get; set; } = [];
}

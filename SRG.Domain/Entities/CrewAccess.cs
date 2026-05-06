namespace SRG.Domain.Entities;

/// <summary>
/// Przypisanie dostępu użytkownika (PM lub Subcontractor) do brygady.
/// Zarządzane przez Admina.
/// </summary>
public class CrewAccess
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid CrewId { get; set; }
    public Crew? Crew { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public Guid AssignedById { get; set; }
    public User? AssignedBy { get; set; }
}

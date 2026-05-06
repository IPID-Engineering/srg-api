namespace SRG.Domain.Entities;

/// <summary>
/// Reprezentuje dostęp PM do brygady podwykonawcy.
/// Zarządzane przez Subcontractora - właściciela brygady.
/// </summary>
public class SubcontractorCrewPmAccess
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid CrewId { get; set; }
    public SubcontractorCrew? Crew { get; set; }
    
    /// <summary>
    /// ID użytkownika PM, który otrzymał dostęp.
    /// </summary>
    public Guid PmUserId { get; set; }
    public User? PmUser { get; set; }
    
    /// <summary>
    /// ID podwykonawcy, który nadał dostęp.
    /// </summary>
    public Guid GrantedBySubcontractorId { get; set; }
    public User? GrantedBySubcontractor { get; set; }
    
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}

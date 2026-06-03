namespace SRG.Domain.Entities;

public class SubcontractorWorker
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    
    /// <summary>
    /// Email jest opcjonalny dla pracownika, ale wymagany gdy pracownik zostaje brygadzistą.
    /// Służy jako login do panelu brygadzisty.
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Hash hasła - ustawiany automatycznie gdy pracownik zostaje brygadzistą.
    /// </summary>
    public string? PasswordHash { get; set; }
    
    /// <summary>
    /// Flaga wymuszająca zmianę hasła przy pierwszym logowaniu brygadzisty.
    /// Po ustawieniu hasła przez brygadzistę zmienia się na false.
    /// </summary>
    public bool MustChangePassword { get; set; }
    
    /// <summary>
    /// Domyślne hasło (niezahaszowane) - widoczne dla subco do momentu pierwszego logowania brygadzisty.
    /// Po zmianie hasła przez brygadzistę pole jest czyszczone.
    /// </summary>
    public string? DefaultPassword { get; set; }
    
    public Guid SubcontractorId { get; set; }
    public User? Subcontractor { get; set; }
    public Guid? CrewId { get; set; }
    public SubcontractorCrew? Crew { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// ID pracownika w systemie inewi (do integracji RCP).
    /// Ustawiany przez podwykonawcę przy mapowaniu pracowników.
    /// </summary>
    public string? InewiEmployeeId { get; set; }
    
    /// <summary>
    /// Grupa stawek przypisana do pracownika.
    /// </summary>
    public Guid? RateGroupId { get; set; }
    public RateGroup? RateGroup { get; set; }
    
    public ICollection<WorkHour> WorkHours { get; set; } = [];
    public ICollection<SubcontractorForemanHistory> ForemanHistory { get; set; } = [];
}

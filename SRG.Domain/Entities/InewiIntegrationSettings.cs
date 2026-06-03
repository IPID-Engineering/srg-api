namespace SRG.Domain.Entities;

/// <summary>
/// Konfiguracja integracji z systemem inewi dla podwykonawcy.
/// Przechowuje dane logowania i token dostępu.
/// Globalna dla całego podwykonawcy, nie powiązana z konkretną brygadą.
/// </summary>
public class InewiIntegrationSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Podwykonawca, dla którego skonfigurowano integrację.
    /// </summary>
    public Guid SubcontractorId { get; set; }
    public User? Subcontractor { get; set; }
    
    /// <summary>
    /// Email do logowania w systemie inewi.
    /// </summary>
    public required string Email { get; set; }
    
    /// <summary>
    /// Hasło do logowania w systemie inewi (zaszyfrowane).
    /// </summary>
    public required string EncryptedPassword { get; set; }
    
    /// <summary>
    /// Aktualny token dostępu do API inewi.
    /// </summary>
    public string? AccessToken { get; set; }
    
    /// <summary>
    /// Data wygaśnięcia tokena.
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }
    
    /// <summary>
    /// Czy integracja jest aktywna.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Ostatnia synchronizacja danych.
    /// </summary>
    public DateTime? LastSyncAt { get; set; }
    
    /// <summary>
    /// Ostatni błąd synchronizacji (jeśli był).
    /// </summary>
    public string? LastError { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Użytkownik, który skonfigurował integrację.
    /// </summary>
    public Guid ConfiguredById { get; set; }
    public User? ConfiguredBy { get; set; }
}

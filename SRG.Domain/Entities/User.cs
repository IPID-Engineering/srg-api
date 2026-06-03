using SRG.Domain.Enums;

namespace SRG.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public string? PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public DateTime? BannedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Microsoft SSO fields
    public string? MicrosoftSubjectId { get; set; }
    public string? ActivationToken { get; set; }
    public DateTime? ActivationTokenExpiresAt { get; set; }
    public bool IsMicrosoftLinked { get; set; }

    // One-time login token (for admin-generated temporary access)
    public string? OneTimeLoginToken { get; set; }
    public DateTime? OneTimeLoginTokenExpiresAt { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}

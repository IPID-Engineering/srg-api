using SRG.Domain.Enums;

namespace SRG.Application.Auth;

public record LoginRequest(string Email, string Password);

public record AuthResponse(Guid UserId, string Email, UserRole Role, string Token);

public record CreateUserRequest(string Email, string Password, string FirstName, string LastName, UserRole Role);

public record CreateForemanRequest(string Email, string Password, string FirstName, string LastName);

public record UpdateUserRequest(string? Email, string? FirstName, string? LastName, UserRole? Role);

public record UserResponse(
    Guid Id, 
    string Email, 
    string FirstName, 
    string LastName, 
    string FullName, 
    UserRole Role, 
    bool IsActive, 
    DateTime CreatedAt, 
    bool IsMicrosoftLinked = false, 
    bool HasActivationToken = false,
    bool IsBanned = false,
    string? BanReason = null);

public record BanUserRequest(string? Reason);
public record UnbanUserRequest();

// One-time login token
public record OneTimeTokenResponse(string Token, DateTime ExpiresAt);
public record OneTimeLoginRequest(string Token);

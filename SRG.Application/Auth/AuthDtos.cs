using SRG.Domain.Enums;

namespace SRG.Application.Auth;

public record LoginRequest(string Email, string Password);

public record AuthResponse(Guid UserId, string Email, UserRole Role, string Token);

public record CreateUserRequest(string Email, string Password, string FirstName, string LastName, UserRole Role);

public record CreateForemanRequest(string Email, string Password, string FirstName, string LastName);

public record UpdateUserRequest(string? Email, string? FirstName, string? LastName, UserRole? Role);

public record UserResponse(Guid Id, string Email, string FirstName, string LastName, string FullName, UserRole Role, bool IsActive, DateTime CreatedAt);

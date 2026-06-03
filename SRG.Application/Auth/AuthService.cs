using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using SRG.Application.Audit;
using SRG.Application.Common;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.Auth;

public class AuthService(
    IUserRepository users,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService,
    IAuditService auditService,
    ICurrentUserContext currentUserContext) : IAuthService
{
    private static readonly UserRole[] AdminAssignableRoles =
    {
        UserRole.PM,
        UserRole.SPM,
        UserRole.Logistician,
        UserRole.Subcontractor,
    };

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.IsActive || user.PasswordHash is null || !passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (user.IsBanned)
        {
            throw new UnauthorizedAccessException("Account has been banned.");
        }

        var token = jwtTokenService.CreateToken(user);

        return new AuthResponse(user.Id, user.Email, user.Role, token);
    }

    public async Task<UserResponse> RegisterUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!AdminAssignableRoles.Contains(request.Role))
        {
            throw new ValidationException("Admin can create PM, SPM, Logistician, and Subcontractor users only.");
        }

        return await CreateUserAsync(request.Email, request.Password, request.FirstName, request.LastName, request.Role, cancellationToken);
    }

    public async Task<UserResponse> RegisterForemanAsync(
        CreateForemanRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CreateUserAsync(request.Email, request.Password, request.FirstName, request.LastName, UserRole.Foreman, cancellationToken);
    }

    public async Task<List<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var allUsers = await users.GetAllAsync(cancellationToken);
        return allUsers.Select(ToResponse).ToList();
    }

    public async Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        if (request.Email is not null)
        {
            var normalizedEmail = NormalizeEmail(request.Email);
            if (normalizedEmail != user.Email && await users.ExistsByEmailAsync(normalizedEmail, cancellationToken))
            {
                throw new ValidationException("A user with this email already exists.");
            }
            user.Email = normalizedEmail;
        }

        if (request.FirstName is not null)
        {
            user.FirstName = request.FirstName.Trim();
        }

        if (request.LastName is not null)
        {
            user.LastName = request.LastName.Trim();
        }

        if (request.Role is not null)
        {
            user.Role = request.Role.Value;
        }

        users.Update(user);
        await users.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? Guid.Empty, "UPDATE_USER", "User", user.Id, new
        {
            user.Email,
            user.FirstName,
            user.LastName,
            Role = user.Role.ToString(),
        }, cancellationToken);

        return ToResponse(user);
    }

    public async Task<UserResponse> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        user.IsActive = false;
        users.Update(user);
        await users.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? Guid.Empty, "DEACTIVATE_USER", "User", user.Id, null, cancellationToken);

        return ToResponse(user);
    }

    public async Task<UserResponse> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        user.IsActive = true;
        users.Update(user);
        await users.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? Guid.Empty, "ACTIVATE_USER", "User", user.Id, null, cancellationToken);

        return ToResponse(user);
    }

    private async Task<UserResponse> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (await users.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new ValidationException("A user with this email already exists.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ValidationException("Password must be at least 8 characters long.");
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ValidationException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ValidationException("Last name is required.");
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordService.HashPassword(password),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? Guid.Empty, "CREATE_USER", "User", user.Id, new
        {
            user.Email,
            user.FirstName,
            user.LastName,
            Role = user.Role.ToString(),
        }, cancellationToken);

        return ToResponse(user);
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        return email.Trim().ToLowerInvariant();
    }

    public async Task<UserResponse> BanUserAsync(Guid userId, BanUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        if (user.Role == UserRole.Admin)
        {
            throw new ValidationException("Cannot ban an admin user.");
        }

        user.IsBanned = true;
        user.BanReason = request.Reason;
        user.BannedAt = DateTime.UtcNow;
        
        users.Update(user);
        await users.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? Guid.Empty, "BAN_USER", "User", user.Id, new { request.Reason }, cancellationToken);

        return ToResponse(user);
    }

    public async Task<UserResponse> UnbanUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        user.IsBanned = false;
        user.BanReason = null;
        user.BannedAt = null;
        
        users.Update(user);
        await users.SaveChangesAsync(cancellationToken);
        await auditService.LogActionAsync(currentUserContext.UserId ?? Guid.Empty, "UNBAN_USER", "User", user.Id, null, cancellationToken);

        return ToResponse(user);
    }

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse(
            user.Id, 
            user.Email, 
            user.FirstName, 
            user.LastName, 
            user.FullName, 
            user.Role, 
            user.IsActive, 
            user.CreatedAt,
            user.IsMicrosoftLinked,
            user.ActivationToken != null && user.ActivationTokenExpiresAt > DateTime.UtcNow,
            user.IsBanned,
            user.BanReason);
    }

    public async Task<OneTimeTokenResponse> GenerateOneTimeLoginTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        if (!user.IsActive)
        {
            throw new ValidationException("Cannot generate token for inactive user.");
        }

        if (user.IsBanned)
        {
            throw new ValidationException("Cannot generate token for banned user.");
        }

        var token = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        user.OneTimeLoginToken = token;
        user.OneTimeLoginTokenExpiresAt = expiresAt;

        users.Update(user);
        await users.SaveChangesAsync(cancellationToken);

        await auditService.LogActionAsync(
            currentUserContext.UserId ?? Guid.Empty,
            "GENERATE_ONE_TIME_TOKEN",
            "User",
            user.Id,
            new { ExpiresAt = expiresAt },
            cancellationToken);

        return new OneTimeTokenResponse(token, expiresAt);
    }

    public async Task<AuthResponse> LoginWithOneTimeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException("Token is required.");
        }

        var user = await users.GetByOneTimeLoginTokenAsync(token.Trim(), cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid or expired token.");
        }

        if (user.OneTimeLoginTokenExpiresAt < DateTime.UtcNow)
        {
            // Clear expired token
            user.OneTimeLoginToken = null;
            user.OneTimeLoginTokenExpiresAt = null;
            users.Update(user);
            await users.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Token has expired.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is not active.");
        }

        if (user.IsBanned)
        {
            throw new UnauthorizedAccessException("Account has been banned.");
        }

        // Clear the token after successful use (one-time use)
        user.OneTimeLoginToken = null;
        user.OneTimeLoginTokenExpiresAt = null;
        users.Update(user);
        await users.SaveChangesAsync(cancellationToken);

        await auditService.LogActionAsync(
            user.Id,
            "ONE_TIME_TOKEN_LOGIN",
            "User",
            user.Id,
            null,
            cancellationToken);

        var jwtToken = jwtTokenService.CreateToken(user);
        return new AuthResponse(user.Id, user.Email, user.Role, jwtToken);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

using System.ComponentModel.DataAnnotations;
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

        if (user is null || !user.IsActive || !passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
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

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.Role, user.IsActive, user.CreatedAt);
    }
}

using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using SRG.Application.Audit;
using SRG.Application.Common;
using SRG.Application.Email;
using SRG.Application.Persistence;

namespace SRG.Application.Auth;

public interface IMicrosoftAuthService
{
    Task<ActivationTokenResponse> GenerateActivationTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ActivationTokenResponse> ResetActivationTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthResponse> ActivateWithMicrosoftAsync(ActivateWithMicrosoftRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginWithMicrosoftAsync(MicrosoftLoginRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetUserByActivationTokenAsync(string token, CancellationToken cancellationToken = default);
}

public class MicrosoftAuthService(
    IUserRepository users,
    IJwtTokenService jwtTokenService,
    IAuditService auditService,
    ICurrentUserContext currentUserContext,
    IEmailService emailService,
    IConfiguration configuration) : IMicrosoftAuthService
{
    private readonly string _clientId = configuration["MicrosoftAuth:ClientId"] 
        ?? throw new InvalidOperationException("MicrosoftAuth:ClientId not configured");
    private readonly int _tokenExpirationMinutes = int.Parse(configuration["MicrosoftAuth:ActivationTokenExpirationMinutes"] ?? "5");

    private static readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager = new(
        "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever());

    public async Task<ActivationTokenResponse> GenerateActivationTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        if (user.IsMicrosoftLinked)
        {
            throw new ValidationException("User is already linked to Microsoft account.");
        }

        return await GenerateAndSendTokenAsync(user, "GENERATE_ACTIVATION_TOKEN", cancellationToken);
    }

    public async Task<ActivationTokenResponse> ResetActivationTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        // Reset: clear Microsoft link if exists, generate new token
        user.MicrosoftSubjectId = null;
        user.IsMicrosoftLinked = false;

        return await GenerateAndSendTokenAsync(user, "RESET_ACTIVATION_TOKEN", cancellationToken);
    }

    private async Task<ActivationTokenResponse> GenerateAndSendTokenAsync(Domain.Entities.User user, string auditAction, CancellationToken cancellationToken)
    {
        var token = GenerateSecureToken();
        user.ActivationToken = token;
        user.ActivationTokenExpiresAt = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes);

        users.Update(user);
        await users.SaveChangesAsync(cancellationToken);

        await auditService.LogActionAsync(
            currentUserContext.UserId ?? Guid.Empty,
            auditAction,
            "User",
            user.Id,
            new { ExpiresAt = user.ActivationTokenExpiresAt },
            cancellationToken);

        // Send activation email (non-blocking - returns false if failed)
        var emailSent = await emailService.SendActivationEmailAsync(
            user.Email,
            user.FullName,
            token,
            user.ActivationTokenExpiresAt.Value,
            cancellationToken);

        return new ActivationTokenResponse(token, user.ActivationTokenExpiresAt.Value, emailSent);
    }

    public async Task<UserResponse?> GetUserByActivationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByActivationTokenAsync(token, cancellationToken);
        
        if (user is null || user.ActivationTokenExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        return new UserResponse(
            user.Id, 
            user.Email, 
            user.FirstName, 
            user.LastName, 
            user.FullName, 
            user.Role, 
            user.IsActive, 
            user.CreatedAt);
    }

    public async Task<AuthResponse> ActivateWithMicrosoftAsync(ActivateWithMicrosoftRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByActivationTokenAsync(request.ActivationToken, cancellationToken)
            ?? throw new ValidationException("Invalid or expired activation token.");

        if (user.ActivationTokenExpiresAt < DateTime.UtcNow)
        {
            throw new ValidationException("Activation token has expired.");
        }

        if (!user.IsActive)
        {
            throw new ValidationException("User account is not active.");
        }

        var microsoftClaims = await ValidateMicrosoftTokenAsync(request.MicrosoftIdToken, cancellationToken);

        var microsoftEmail = microsoftClaims.Email?.ToLowerInvariant();
        if (string.IsNullOrEmpty(microsoftEmail) || microsoftEmail != user.Email.ToLowerInvariant())
        {
            throw new ValidationException($"Microsoft account email ({microsoftEmail}) does not match the user's email ({user.Email}).");
        }

        var existingUserWithMicrosoftId = await users.GetByMicrosoftSubjectIdAsync(microsoftClaims.SubjectId, cancellationToken);
        if (existingUserWithMicrosoftId != null && existingUserWithMicrosoftId.Id != user.Id)
        {
            throw new ValidationException("This Microsoft account is already linked to another user.");
        }

        user.MicrosoftSubjectId = microsoftClaims.SubjectId;
        user.IsMicrosoftLinked = true;
        user.ActivationToken = null;
        user.ActivationTokenExpiresAt = null;
        user.PasswordHash = null;

        users.Update(user);
        await users.SaveChangesAsync(cancellationToken);

        await auditService.LogActionAsync(
            user.Id,
            "MICROSOFT_ACCOUNT_LINKED",
            "User",
            user.Id,
            new { MicrosoftEmail = microsoftEmail },
            cancellationToken);

        var token = jwtTokenService.CreateToken(user);
        return new AuthResponse(user.Id, user.Email, user.Role, token);
    }

    public async Task<AuthResponse> LoginWithMicrosoftAsync(MicrosoftLoginRequest request, CancellationToken cancellationToken = default)
    {
        var microsoftClaims = await ValidateMicrosoftTokenAsync(request.MicrosoftIdToken, cancellationToken);

        var user = await users.GetByMicrosoftSubjectIdAsync(microsoftClaims.SubjectId, cancellationToken);
        
        if (user is null)
        {
            user = await users.GetByEmailAsync(microsoftClaims.Email?.ToLowerInvariant() ?? "", cancellationToken);
            if (user is null)
            {
                throw new UnauthorizedAccessException("No account found for this Microsoft account. Please contact administrator.");
            }
            
            if (!user.IsMicrosoftLinked)
            {
                throw new UnauthorizedAccessException("Account not yet activated with Microsoft. Please use activation token first.");
            }
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is not active.");
        }

        if (user.IsBanned)
        {
            throw new UnauthorizedAccessException("Account has been banned.");
        }

        var token = jwtTokenService.CreateToken(user);
        return new AuthResponse(user.Id, user.Email, user.Role, token);
    }

    private async Task<MicrosoftTokenClaims> ValidateMicrosoftTokenAsync(string idToken, CancellationToken cancellationToken)
    {
        var config = await _configManager.GetConfigurationAsync(cancellationToken);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new[]
            {
                "https://login.microsoftonline.com/common/v2.0",
                "https://login.microsoftonline.com/consumers/v2.0",
                "https://login.microsoftonline.com/organizations/v2.0",
                // Personal Microsoft accounts (MSA) tenant
                "https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0",
                $"https://login.microsoftonline.com/{configuration["MicrosoftAuth:TenantId"]}/v2.0"
            },
            ValidateAudience = true,
            ValidAudience = _clientId,
            ValidateLifetime = true,
            IssuerSigningKeys = config.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(idToken, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                throw new ValidationException("Invalid token format.");
            }

            var subjectId = jwtToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value 
                ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                ?? throw new ValidationException("Token missing subject identifier.");

            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;

            return new MicrosoftTokenClaims(subjectId, email);
        }
        catch (SecurityTokenException ex)
        {
            throw new ValidationException($"Microsoft token validation failed: {ex.Message}");
        }
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private record MicrosoftTokenClaims(string SubjectId, string? Email);
}

public record ActivationTokenResponse(string Token, DateTime ExpiresAt, bool EmailSent);
public record ActivateWithMicrosoftRequest(string ActivationToken, string MicrosoftIdToken);
public record MicrosoftLoginRequest(string MicrosoftIdToken);

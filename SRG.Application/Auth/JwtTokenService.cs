using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SRG.Application.Common;
using SRG.Domain.Entities;

namespace SRG.Application.Auth;

public class JwtTokenService(IOptions<JwtSettings> jwtOptions) : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim("userId", user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        return GenerateToken(claims);
    }

    public string CreateForemanToken(SubcontractorWorker worker)
    {
        var claims = new[]
        {
            new Claim("userId", worker.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, worker.Id.ToString()),
            new Claim("email", worker.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Email, worker.Email ?? string.Empty),
            new Claim("role", "SubcontractorForeman"),
            new Claim(ClaimTypes.Role, "SubcontractorForeman"),
            new Claim("crewId", worker.CrewId?.ToString() ?? string.Empty),
            new Claim("subcontractorId", worker.SubcontractorId.ToString()),
        };

        return GenerateToken(claims);
    }

    private string GenerateToken(Claim[] claims)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

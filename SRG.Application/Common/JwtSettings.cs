namespace SRG.Application.Common;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string SecretKey { get; init; }
    public int ExpirationMinutes { get; init; } = 480;
}

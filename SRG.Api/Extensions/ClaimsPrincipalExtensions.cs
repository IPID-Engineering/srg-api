using System.Security.Claims;

namespace SRG.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("JWT userId claim is missing.");

        return Guid.Parse(userId);
    }

    public static Guid? GetCrewId(this ClaimsPrincipal user)
    {
        var crewId = user.FindFirstValue("crewId");
        if (string.IsNullOrEmpty(crewId))
            return null;
        return Guid.Parse(crewId);
    }

    public static Guid GetCrewIdRequired(this ClaimsPrincipal user)
    {
        var crewId = user.FindFirstValue("crewId")
            ?? throw new UnauthorizedAccessException("JWT crewId claim is missing.");
        return Guid.Parse(crewId);
    }
}

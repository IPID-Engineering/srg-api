using System.Security.Claims;
using SRG.Application.Common;

namespace SRG.Api.Services;

public class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public Guid? UserId
    {
        get
        {
            // Try userId claim first (for Users), then sub claim (for SubcontractorWorkers)
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue("userId")
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }
    
    public string? Role => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
    
    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue("email")
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);
}

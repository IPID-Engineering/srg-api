using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Application.Auth;
using SRG.Api.Extensions;

namespace SRG.Api.Controllers;

[ApiController]
[Route("foreman-auth")]
public class ForemanAuthController(IForemanAuthService foremanAuthService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ForemanAuthResponse>> Login(
        ForemanLoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await foremanAuthService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("change-password")]
    [Authorize(Roles = "SubcontractorForeman")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var workerId = User.GetUserId();
        await foremanAuthService.ChangePasswordAsync(workerId, request, cancellationToken);
        return Ok(new { message = "Hasło zostało zmienione." });
    }
}

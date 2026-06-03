using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Application.Auth;

namespace SRG.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService, IMicrosoftAuthService microsoftAuthService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.LoginAsync(request, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }
    }

    [HttpPost("create-user")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponse>> CreateUser(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await authService.RegisterUserAsync(request, cancellationToken));
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("create-foreman")]
    [Authorize(Roles = "PM")]
    public async Task<ActionResult<UserResponse>> CreateForeman(
        CreateForemanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Created(string.Empty, await authService.RegisterForemanAsync(request, cancellationToken));
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserResponse>>> GetAllUsers(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.GetAllUsersAsync(cancellationToken));
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się pobrać listy użytkowników." });
        }
    }

    [HttpGet("users/pm-list")]
    [Authorize(Roles = "Subcontractor")]
    public async Task<ActionResult<List<UserResponse>>> GetPmUsers(CancellationToken cancellationToken)
    {
        try
        {
            var allUsers = await authService.GetAllUsersAsync(cancellationToken);
            var pmUsers = allUsers.Where(u => u.Role == SRG.Domain.Enums.UserRole.PM).ToList();
            return Ok(pmUsers);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Nie udało się pobrać listy PM." });
        }
    }

    [HttpPut("users/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponse>> UpdateUser(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.UpdateUserAsync(id, request, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("users/{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponse>> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.DeactivateUserAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("users/{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponse>> ActivateUser(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.ActivateUserAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("users/{id:guid}/ban")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponse>> BanUser(
        Guid id, 
        [FromBody] BanUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.BanUserAsync(id, request, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("users/{id:guid}/unban")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponse>> UnbanUser(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.UnbanUserAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    // Microsoft SSO endpoints

    [HttpPost("microsoft/generate-token/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ActivationTokenResponse>> GenerateMicrosoftActivationToken(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await microsoftAuthService.GenerateActivationTokenAsync(userId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("microsoft/reset-token/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ActivationTokenResponse>> ResetMicrosoftActivationToken(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await microsoftAuthService.ResetActivationTokenAsync(userId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("microsoft/validate-token")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponse>> ValidateActivationToken(
        [FromBody] ValidateTokenRequest request,
        CancellationToken cancellationToken)
    {
        var user = await microsoftAuthService.GetUserByActivationTokenAsync(request.Token, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Invalid or expired activation token." });
        }
        return Ok(user);
    }

    [HttpPost("microsoft/activate")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> ActivateWithMicrosoft(
        ActivateWithMicrosoftRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await microsoftAuthService.ActivateWithMicrosoftAsync(request, cancellationToken));
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }

    [HttpPost("microsoft/login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> LoginWithMicrosoft(
        MicrosoftLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await microsoftAuthService.LoginWithMicrosoftAsync(request, cancellationToken));
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }

    // One-time login token endpoints

    [HttpPost("one-time-token/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OneTimeTokenResponse>> GenerateOneTimeLoginToken(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.GenerateOneTimeLoginTokenAsync(userId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("one-time-login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> LoginWithOneTimeToken(
        OneTimeLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authService.LoginWithOneTimeTokenAsync(request.Token, cancellationToken));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }
}

public record ValidateTokenRequest(string Token);

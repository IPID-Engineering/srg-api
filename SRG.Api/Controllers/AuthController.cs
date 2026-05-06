using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRG.Application.Auth;

namespace SRG.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService) : ControllerBase
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
    [Authorize(Roles = "Admin,Subcontractor")]
    public async Task<ActionResult<List<UserResponse>>> GetAllUsers(CancellationToken cancellationToken)
    {
        return Ok(await authService.GetAllUsersAsync(cancellationToken));
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
}

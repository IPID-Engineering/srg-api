using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Auth;

public interface IForemanAuthService
{
    Task<ForemanAuthResponse> LoginAsync(ForemanLoginRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid workerId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

public record ForemanLoginRequest(string Email, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record ForemanAuthResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    Guid CrewId,
    string CrewName,
    string Token,
    /// <summary>
    /// Jeśli true, brygadzista musi zmienić hasło przed uzyskaniem pełnego dostępu.
    /// Frontend powinien pokazać modal zmiany hasła, którego nie można zamknąć.
    /// </summary>
    bool MustChangePassword);

public class ForemanAuthService(
    IConstructionRepository construction,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService) : IForemanAuthService
{
    public async Task<ForemanAuthResponse> LoginAsync(ForemanLoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant()
            ?? throw new ValidationException("Email jest wymagany.");

        var worker = await construction.GetSubcontractorWorkerByEmailAsync(email, cancellationToken);

        if (worker is null)
        {
            throw new UnauthorizedAccessException("Nieprawidłowy email lub hasło.");
        }

        // Sprawdź czy pracownik jest aktualnie brygadzistą jakiejś brygady
        if (worker.Crew?.CurrentForemanId != worker.Id)
        {
            throw new UnauthorizedAccessException("Ten pracownik nie jest aktualnie brygadzistą.");
        }

        if (string.IsNullOrEmpty(worker.PasswordHash))
        {
            throw new UnauthorizedAccessException("Konto brygadzisty nie zostało jeszcze skonfigurowane.");
        }

        if (!passwordService.VerifyPassword(request.Password, worker.PasswordHash))
        {
            throw new UnauthorizedAccessException("Nieprawidłowy email lub hasło.");
        }

        // Generuj token JWT dla brygadzisty
        var token = jwtTokenService.CreateForemanToken(worker);

        return new ForemanAuthResponse(
            worker.Id,
            worker.Email!,
            worker.FirstName,
            worker.LastName,
            worker.CrewId!.Value,
            worker.Crew.Name,
            token,
            worker.MustChangePassword);
    }

    public async Task ChangePasswordAsync(Guid workerId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var worker = await construction.GetSubcontractorWorkerByIdAsync(workerId, cancellationToken)
            ?? throw new KeyNotFoundException("Nie znaleziono pracownika.");

        if (string.IsNullOrEmpty(worker.PasswordHash))
        {
            throw new ValidationException("Konto brygadzisty nie zostało jeszcze skonfigurowane.");
        }

        if (!passwordService.VerifyPassword(request.CurrentPassword, worker.PasswordHash))
        {
            throw new ValidationException("Aktualne hasło jest nieprawidłowe.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            throw new ValidationException("Nowe hasło musi mieć co najmniej 8 znaków.");
        }

        worker.PasswordHash = passwordService.HashPassword(request.NewPassword);
        worker.MustChangePassword = false;
        // Po zmianie hasła usuwamy domyślne hasło - subco nie może go już zobaczyć
        worker.DefaultPassword = null;

        await construction.SaveChangesAsync(cancellationToken);
    }
}

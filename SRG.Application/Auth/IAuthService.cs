namespace SRG.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> RegisterUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> RegisterForemanAsync(CreateForemanRequest request, CancellationToken cancellationToken = default);
    Task<List<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserResponse> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserResponse> BanUserAsync(Guid userId, BanUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UnbanUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    // One-time login token
    Task<OneTimeTokenResponse> GenerateOneTimeLoginTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginWithOneTimeTokenAsync(string token, CancellationToken cancellationToken = default);
}

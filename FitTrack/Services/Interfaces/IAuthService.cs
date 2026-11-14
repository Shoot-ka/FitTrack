using FitTrack.DTOs.Auth;

namespace FitTrack.Services.Interfaces;

public interface IAuthService
{
    Task<RegisteredUserDto?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

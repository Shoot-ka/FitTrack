namespace FitTrack.DTOs.Auth;

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt);

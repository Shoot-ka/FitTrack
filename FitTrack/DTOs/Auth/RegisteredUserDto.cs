namespace FitTrack.DTOs.Auth;

public sealed record RegisteredUserDto(int Id, string Email, string FullName, string Role);

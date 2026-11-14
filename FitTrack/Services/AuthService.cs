using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FitTrack.Data;
using FitTrack.DTOs.Auth;
using FitTrack.Enums;
using FitTrack.Model;
using FitTrack.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FitTrack.Services;

public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    TokenValidationParameters jwt) : IAuthService
{
    public async Task<RegisteredUserDto?> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Role = UserRole.Member,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return null;
        }

        return new RegisteredUserDto(user.Id, user.Email, user.FullName, user.Role.ToString());
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var expiresAt = DateTime.UtcNow.AddHours(8);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString())
        };

        var token = new JwtSecurityToken(jwt.ValidIssuer, jwt.ValidAudience, claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(jwt.IssuerSigningKey, SecurityAlgorithms.HmacSha256));

        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

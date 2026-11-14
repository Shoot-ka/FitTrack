using FitTrack.Enums;
using FitTrack.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedTrainerAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["SeedTrainer:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["SeedTrainer:Password"];

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrEmpty(password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Set both SeedTrainer:Email and SeedTrainer:Password.");
        }

        var db = services.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleOrDefaultAsync(user => user.Email == email);
        if (user is not null)
        {
            if (user.Role != UserRole.Trainer)
            {
                throw new InvalidOperationException("The configured Trainer email belongs to a Member.");
            }

            return;
        }

        var trainer = new User
        {
            Email = email,
            FullName = "FitTrack Trainer",
            Role = UserRole.Trainer,
            CreatedAt = DateTime.UtcNow
        };
        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        trainer.PasswordHash = hasher.HashPassword(trainer, password);

        db.Users.Add(trainer);
        await db.SaveChangesAsync();
    }
}

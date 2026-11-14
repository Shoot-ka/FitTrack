using FitTrack.Model;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Data;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IConfiguration configuration) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<BodyMetrics> BodyMetrics => Set<BodyMetrics>();
    public DbSet<MuscleGroup> MuscleGroups => Set<MuscleGroup>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<WorkoutSet> WorkoutSets => Set<WorkoutSet>();
    public DbSet<TrainerClient> TrainerClients => Set<TrainerClient>();
    public DbSet<ExerciseMuscleGroup> ExerciseMuscleGroups => Set<ExerciseMuscleGroup>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var connectionString = configuration.GetConnectionString("FitTrack")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:FitTrack before starting the API.");

        optionsBuilder.UseSqlServer(connectionString);
    }

protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        FitTrackModelConfiguration.Configure(modelBuilder);
    }
}

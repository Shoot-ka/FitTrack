using FitTrack.Model;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Data;

internal static class FitTrackModelConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.Property(x => x.Email).HasMaxLength(254);
            builder.HasIndex(x => x.Email).IsUnique();
            builder.Property(x => x.FullName).HasMaxLength(100);
            builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(16);
        });

        modelBuilder.Entity<BodyMetrics>(builder =>
        {
            builder.Property(x => x.HeightCm).HasPrecision(5, 2);
            builder.Property(x => x.WeightKg).HasPrecision(5, 2);
            builder.Property(x => x.Goal).HasConversion<string>().HasMaxLength(16);
            builder.HasOne(x => x.User).WithOne(x => x.BodyMetrics)
                .HasForeignKey<BodyMetrics>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.UserId).IsUnique();
        });

        modelBuilder.Entity<MuscleGroup>(builder =>
        {
            builder.Property(x => x.Name).HasMaxLength(80);
            builder.HasIndex(x => x.Name).IsUnique();
            builder.HasData(
                new MuscleGroup { Id = 1, Name = "Chest" },
                new MuscleGroup { Id = 2, Name = "Back" },
                new MuscleGroup { Id = 3, Name = "Legs" });
        });

        modelBuilder.Entity<Exercise>(builder =>
        {
            builder.Property(x => x.Name).HasMaxLength(80);
            builder.HasIndex(x => x.Name).IsUnique();
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.Equipment).HasConversion<string>().HasMaxLength(16);
        });

        modelBuilder.Entity<Workout>(builder =>
        {
            builder.Property(x => x.Title).HasMaxLength(80);
            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.HasOne(x => x.User).WithMany(x => x.Workouts)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.UserId, x.Date });
        });

        modelBuilder.Entity<WorkoutSet>(builder =>
        {
            builder.Property(x => x.WeightKg).HasPrecision(5, 2);
            builder.HasOne(x => x.Workout).WithMany(x => x.Sets)
                .HasForeignKey(x => x.WorkoutId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Exercise).WithMany(x => x.WorkoutSets)
                .HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.WorkoutId, x.ExerciseId, x.SetNumber }).IsUnique();
        });

        modelBuilder.Entity<TrainerClient>(builder =>
        {
            builder.HasKey(x => new { x.TrainerId, x.ClientId });
            builder.HasOne(x => x.Trainer).WithMany(x => x.Clients)
                .HasForeignKey(x => x.TrainerId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Client).WithMany(x => x.Trainers)
                .HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExerciseMuscleGroup>(builder =>
        {
            builder.ToTable("ExerciseMuscleGroup");
            builder.HasKey(link => new { link.ExerciseId, link.MuscleGroupId });
            builder.HasOne(link => link.Exercise).WithMany(exercise => exercise.ExerciseMuscleGroups)
                .HasForeignKey(link => link.ExerciseId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(link => link.MuscleGroup).WithMany(group => group.ExerciseMuscleGroups)
                .HasForeignKey(link => link.MuscleGroupId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

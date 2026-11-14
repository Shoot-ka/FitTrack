using Microsoft.EntityFrameworkCore;

namespace FitTrack.Model;

public sealed class WorkoutSet
{
    public int Id { get; set; }
    public int WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;
    public int ExerciseId { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Exercise Exercise { get; set; } = null!;
    public int SetNumber { get; set; }
    public int Reps { get; set; }
    [Precision(5, 2)]
    public decimal WeightKg { get; set; }
}

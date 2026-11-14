namespace FitTrack.DTOs.Workouts;

public sealed record WorkoutSetRequest(int ExerciseId, int SetNumber, int Reps, decimal WeightKg);

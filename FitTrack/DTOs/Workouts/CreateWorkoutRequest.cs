namespace FitTrack.DTOs.Workouts;

public sealed record CreateWorkoutRequest(DateTimeOffset Date, string? Title, string? Notes,
    int DurationMinutes, List<WorkoutSetRequest?>? Sets);

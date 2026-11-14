namespace FitTrack.DTOs.Workouts;

public sealed record UpdateWorkoutRequest(DateTimeOffset Date, string? Title, string? Notes,
    int DurationMinutes, List<WorkoutSetRequest?>? Sets);

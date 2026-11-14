namespace FitTrack.DTOs.Workouts;

public sealed record WorkoutExerciseDto(
    int ExerciseId, string ExerciseName, IReadOnlyList<WorkoutSetDto> Sets);

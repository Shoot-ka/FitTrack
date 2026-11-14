using FitTrack.DTOs.Stats;

namespace FitTrack.Services.Interfaces;

public interface IStatsService
{
    Task<IReadOnlyList<WeeklyStatsDto>> GetWeeklyAsync(int userId, int weeks, CancellationToken ct);
    Task<IReadOnlyList<ExerciseProgressDto>> GetExerciseProgressAsync(int userId, int exerciseId, CancellationToken ct);
}

using FitTrack.Data;
using FitTrack.DTOs.Common;
using FitTrack.DTOs.Stats;
using FitTrack.Exceptions;
using FitTrack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Services;

public sealed class StatsService(AppDbContext db) : IStatsService
{
    public async Task<IReadOnlyList<WeeklyStatsDto>> GetWeeklyAsync(int userId, int weeks, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var currentWeek = StartOfWeek(now);
        var daysToFirstWeek = (weeks - 1L) * 7;
        if (weeks < 1 || daysToFirstWeek > (currentWeek - DateTime.MinValue).Days)
        {
            throw new ApiException(new ErrorResponse(400, "Provide a valid number of weeks."));
        }

        var firstWeek = currentWeek.AddDays(-daysToFirstWeek);
        var workouts = await db.Workouts
            .Where(workout => workout.UserId == userId && workout.Date >= firstWeek && workout.Date <= now)
            .Select(workout => new
            {
                workout.Date,
                workout.DurationMinutes,
                Volume = workout.Sets.Sum(set => set.Reps * set.WeightKg)
            })
            .ToListAsync(ct);

        var totals = workouts.GroupBy(workout => StartOfWeek(workout.Date))
            .ToDictionary(
                group => group.Key,
                group => new WeeklyStatsDto(DateOnly.FromDateTime(group.Key), group.Count(),
                    group.Sum(workout => workout.Volume), group.Sum(workout => workout.DurationMinutes)));

        var result = new List<WeeklyStatsDto>(weeks);
        for (var index = 0; index < weeks; index++)
        {
            var week = firstWeek.AddDays(index * 7);
            result.Add(totals.GetValueOrDefault(week)
                ?? new WeeklyStatsDto(DateOnly.FromDateTime(week), 0, 0m, 0));
        }

        return result;
    }

    public async Task<IReadOnlyList<ExerciseProgressDto>> GetExerciseProgressAsync(
        int userId, int exerciseId, CancellationToken ct)
    {
        if (!await db.Exercises.AnyAsync(exercise => exercise.Id == exerciseId, ct))
        {
            throw new ApiException(new ErrorResponse(404, "Exercise was not found."));
        }

        var days = await db.WorkoutSets
            .Where(set => set.ExerciseId == exerciseId && set.Workout.UserId == userId)
            .GroupBy(set => set.Workout.Date.Date)
            .OrderBy(group => group.Key)
            .Select(group => new { Date = group.Key, MaxWeightKg = group.Max(set => set.WeightKg) })
            .ToListAsync(ct);

        return days.Select(day => new ExerciseProgressDto(DateOnly.FromDateTime(day.Date), day.MaxWeightKg))
            .ToList();
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }
}

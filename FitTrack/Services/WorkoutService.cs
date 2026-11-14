using AutoMapper;
using FitTrack.Data;
using FitTrack.DTOs.Common;
using FitTrack.DTOs.Workouts;
using FitTrack.Exceptions;
using FitTrack.Model;
using FitTrack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Services;

public sealed class WorkoutService(AppDbContext db, IMapper mapper) : IWorkoutService
{
    public async Task<PagedResult<WorkoutDto>> ListAsync(
        int userId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Workouts.AsNoTracking().Where(workout => workout.UserId == userId);
        if (from.HasValue)
        {
            var start = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(workout => workout.Date >= start);
        }

        if (to.HasValue)
        {
            var end = to.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(workout => workout.Date <= end);
        }

        var total = await query.CountAsync(ct);
        var workouts = await query.OrderByDescending(workout => workout.Date)
            .ThenByDescending(workout => workout.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(workout => workout.Sets).ThenInclude(set => set.Exercise).ToListAsync(ct);

        return new PagedResult<WorkoutDto>(mapper.Map<List<WorkoutDto>>(workouts), total, page, pageSize);
    }

    public async Task<WorkoutDto> GetAsync(int id, int userId, bool isTrainer, CancellationToken ct)
    {
        var workout = await FindAsync(id, ct);
        if (workout.UserId != userId &&
            (!isTrainer || !await db.TrainerClients.AnyAsync(
                link => link.TrainerId == userId && link.ClientId == workout.UserId, ct)))
        {
            throw Forbidden();
        }

        return mapper.Map<WorkoutDto>(workout);
    }

    public async Task<WorkoutDto> CreateAsync(int userId, CreateWorkoutRequest request, CancellationToken ct)
    {
        var workout = new Workout { UserId = userId };
        var exercises = await GetExercisesAsync(request.Sets!, ct);
        ApplyDetails(workout, request.Date, request.Title!, request.Notes, request.DurationMinutes);
        AddSets(workout, request.Sets!, exercises);
        db.Workouts.Add(workout);
        await db.SaveChangesAsync(ct);

        return mapper.Map<WorkoutDto>(workout);
    }

    public async Task<WorkoutDto> UpdateAsync(int id, int userId, UpdateWorkoutRequest request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Updating the parent first serializes replacements for the same workout.
        var updated = await db.Workouts.Where(workout => workout.Id == id && workout.UserId == userId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(workout => workout.Date, request.Date.UtcDateTime)
                .SetProperty(workout => workout.Title, request.Title!.Trim())
                .SetProperty(workout => workout.Notes, request.Notes)
                .SetProperty(workout => workout.DurationMinutes, request.DurationMinutes), ct);
        if (updated == 0)
        {
            var existing = await FindAsync(id, ct);
            throw existing.UserId != userId ? Forbidden() : Missing();
        }

        var exercises = await GetExercisesAsync(request.Sets!, ct);
        await db.WorkoutSets.Where(set => set.WorkoutId == id).ExecuteDeleteAsync(ct);
        var workout = await FindAsync(id, ct);
        AddSets(workout, request.Sets!, exercises);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return mapper.Map<WorkoutDto>(workout);
    }

    public async Task DeleteAsync(int id, int userId, CancellationToken ct)
    {
        var workout = await FindAsync(id, ct);
        if (workout.UserId != userId)
        {
            throw Forbidden();
        }

        db.Workouts.Remove(workout);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Workout> FindAsync(int id, CancellationToken ct) =>
        await db.Workouts.Include(workout => workout.Sets).ThenInclude(set => set.Exercise)
            .SingleOrDefaultAsync(workout => workout.Id == id, ct)
        ?? throw Missing();

    private async Task<Dictionary<int, Exercise>> GetExercisesAsync(
        List<WorkoutSetRequest?> sets, CancellationToken ct)
    {
        var ids = sets.Select(set => set!.ExerciseId).Distinct().ToList();
        var exercises = await db.Exercises.Where(exercise => ids.Contains(exercise.Id))
            .ToDictionaryAsync(exercise => exercise.Id, ct);

        if (exercises.Count != ids.Count)
        {
            throw new ApiException(new ErrorResponse(400, "Validation failed",
                new Dictionary<string, string[]> { ["sets"] = ["All exercises must exist."] }));
        }

        return exercises;
    }

    private static void ApplyDetails(Workout workout, DateTimeOffset date, string title,
        string? notes, int durationMinutes)
    {
        workout.Date = date.UtcDateTime;
        workout.Title = title.Trim();
        workout.Notes = notes;
        workout.DurationMinutes = durationMinutes;
    }

    private static void AddSets(Workout workout, List<WorkoutSetRequest?> sets,
        Dictionary<int, Exercise> exercises)
    {
        foreach (var item in sets)
        {
            var set = item!;
            workout.Sets.Add(new WorkoutSet
            {
                Exercise = exercises[set.ExerciseId],
                ExerciseId = set.ExerciseId,
                SetNumber = set.SetNumber,
                Reps = set.Reps,
                WeightKg = decimal.Round(set.WeightKg, 2, MidpointRounding.AwayFromZero)
            });
        }
    }

    private static ApiException Forbidden() => new(new ErrorResponse(403, "Access to this workout is forbidden."));
    private static ApiException Missing() => new(new ErrorResponse(404, "Workout was not found."));
}

using AutoMapper;
using FitTrack.Data;
using FitTrack.DTOs.Catalog;
using FitTrack.DTOs.Common;
using FitTrack.Enums;
using FitTrack.Exceptions;
using FitTrack.Model;
using FitTrack.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Services;

public sealed class CatalogService(AppDbContext db, IMapper mapper) : ICatalogService
{
    public async Task<PagedResult<MuscleGroupDto>> GetMuscleGroupsAsync(
        int page, int pageSize, CancellationToken ct)
    {
        var query = db.MuscleGroups.AsNoTracking();
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(group => group.Id).Skip((page - 1) * pageSize)
            .Take(pageSize).ToListAsync(ct);

        return new PagedResult<MuscleGroupDto>(mapper.Map<List<MuscleGroupDto>>(rows), total, page, pageSize);
    }

    public async Task<MuscleGroupDto> AddMuscleGroupAsync(
        CreateMuscleGroupRequest request, CancellationToken ct)
    {
        var name = request.Name!.Trim();
        if (await db.MuscleGroups.AnyAsync(group => group.Name == name, ct))
        {
            throw new CatalogException(new ErrorResponse(409, "Muscle group name already exists."));
        }

        var group = new MuscleGroup { Name = name };
        db.MuscleGroups.Add(group);
        await SaveNamedResourceAsync("Muscle group", ct);
        return mapper.Map<MuscleGroupDto>(group);
    }

    public async Task<PagedResult<ExerciseDto>> GetExercisesAsync(
        int? muscleGroupId, Equipment? equipment, string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Exercises.AsNoTracking().AsQueryable();
        if (muscleGroupId.HasValue)
        {
            query = query.Where(exercise =>
                exercise.ExerciseMuscleGroups.Any(link => link.MuscleGroupId == muscleGroupId));
        }

        if (equipment.HasValue)
        {
            query = query.Where(exercise => exercise.Equipment == equipment);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(exercise => exercise.Name.Contains(text));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(exercise => exercise.Id).Skip((page - 1) * pageSize)
            .Take(pageSize).Include(exercise => exercise.ExerciseMuscleGroups)
            .ThenInclude(link => link.MuscleGroup).ToListAsync(ct);

        return new PagedResult<ExerciseDto>(mapper.Map<List<ExerciseDto>>(rows), total, page, pageSize);
    }

    public async Task<ExerciseDto> GetExerciseAsync(int id, CancellationToken ct) =>
        mapper.Map<ExerciseDto>(await FindExerciseAsync(id, ct));

    public async Task<ExerciseDto> AddExerciseAsync(CreateExerciseRequest request, CancellationToken ct)
    {
        var exercise = new Exercise();
        await SetExerciseDetailsAsync(exercise, request.Name!, request.Description,
            request.Equipment!.Value, request.MuscleGroupIds!, ct);
        db.Exercises.Add(exercise);
        await SaveNamedResourceAsync("Exercise", ct);
        return mapper.Map<ExerciseDto>(exercise);
    }

    public async Task<ExerciseDto> UpdateExerciseAsync(int id, UpdateExerciseRequest request, CancellationToken ct)
    {
        var exercise = await FindExerciseAsync(id, ct);
        await SetExerciseDetailsAsync(exercise, request.Name!, request.Description,
            request.Equipment!.Value, request.MuscleGroupIds!, ct);
        await SaveNamedResourceAsync("Exercise", ct);
        return mapper.Map<ExerciseDto>(exercise);
    }

    public async Task DeleteExerciseAsync(int id, CancellationToken ct)
    {
        var exercise = await FindExerciseAsync(id, ct);
        if (await db.WorkoutSets.AnyAsync(set => set.ExerciseId == id, ct))
        {
            throw ExerciseInUse();
        }

        db.Exercises.Remove(exercise);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 547 })
        {
            throw ExerciseInUse();
        }
    }

    private async Task<Exercise> FindExerciseAsync(int id, CancellationToken ct) =>
        await db.Exercises.Include(exercise => exercise.ExerciseMuscleGroups)
            .ThenInclude(link => link.MuscleGroup)
            .SingleOrDefaultAsync(exercise => exercise.Id == id, ct)
        ?? throw new CatalogException(new ErrorResponse(404, "Exercise was not found."));

    private async Task SetExerciseDetailsAsync(Exercise exercise, string name, string? description,
        Equipment equipment, List<int> groupIds, CancellationToken ct)
    {
        name = name.Trim();
        if (await db.Exercises.AnyAsync(item => item.Name == name && item.Id != exercise.Id, ct))
        {
            throw new CatalogException(new ErrorResponse(409, "Exercise name already exists."));
        }

        var groups = await db.MuscleGroups.Where(group => groupIds.Contains(group.Id)).ToListAsync(ct);
        if (groups.Count != groupIds.Count)
        {
            throw new CatalogException(new ErrorResponse(400, "Validation failed",
                new Dictionary<string, string[]> { ["muscleGroupIds"] = ["All muscle groups must exist."] }));
        }

        exercise.Name = name;
        exercise.Description = description;
        exercise.Equipment = equipment;

        var removed = exercise.ExerciseMuscleGroups.Where(link => !groupIds.Contains(link.MuscleGroupId)).ToList();
        foreach (var link in removed)
        {
            exercise.ExerciseMuscleGroups.Remove(link);
            db.ExerciseMuscleGroups.Remove(link);
        }

        var existing = exercise.ExerciseMuscleGroups.Select(link => link.MuscleGroupId).ToHashSet();
        foreach (var group in groups.Where(group => !existing.Contains(group.Id)))
        {
            exercise.ExerciseMuscleGroups.Add(new ExerciseMuscleGroup
            {
                Exercise = exercise,
                MuscleGroup = group,
                MuscleGroupId = group.Id
            });
        }
    }

    private async Task SaveNamedResourceAsync(string resource, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new CatalogException(new ErrorResponse(409, $"{resource} name already exists."));
        }
    }

    private static CatalogException ExerciseInUse() =>
        new(new ErrorResponse(400, "An exercise used in a workout set cannot be deleted."));
}

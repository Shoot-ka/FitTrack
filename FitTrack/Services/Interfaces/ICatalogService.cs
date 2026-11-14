using FitTrack.DTOs.Catalog;
using FitTrack.DTOs.Common;
using FitTrack.Enums;

namespace FitTrack.Services.Interfaces;

public interface ICatalogService
{
    Task<PagedResult<MuscleGroupDto>> GetMuscleGroupsAsync(int page, int pageSize, CancellationToken ct);
    Task<MuscleGroupDto> AddMuscleGroupAsync(CreateMuscleGroupRequest request, CancellationToken ct);
    Task<PagedResult<ExerciseDto>> GetExercisesAsync(
        int? muscleGroupId, Equipment? equipment, string? search, int page, int pageSize, CancellationToken ct);
    Task<ExerciseDto> GetExerciseAsync(int id, CancellationToken ct);
    Task<ExerciseDto> AddExerciseAsync(CreateExerciseRequest request, CancellationToken ct);
    Task<ExerciseDto> UpdateExerciseAsync(int id, UpdateExerciseRequest request, CancellationToken ct);
    Task DeleteExerciseAsync(int id, CancellationToken ct);
}

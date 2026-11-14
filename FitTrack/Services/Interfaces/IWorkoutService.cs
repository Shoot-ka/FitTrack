using FitTrack.DTOs.Common;
using FitTrack.DTOs.Workouts;

namespace FitTrack.Services.Interfaces;

public interface IWorkoutService
{
    Task<PagedResult<WorkoutDto>> ListAsync(
        int userId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct);
    Task<WorkoutDto> GetAsync(int id, int userId, bool isTrainer, CancellationToken ct);
    Task<WorkoutDto> CreateAsync(int userId, CreateWorkoutRequest request, CancellationToken ct);
    Task<WorkoutDto> UpdateAsync(int id, int userId, UpdateWorkoutRequest request, CancellationToken ct);
    Task DeleteAsync(int id, int userId, CancellationToken ct);
}

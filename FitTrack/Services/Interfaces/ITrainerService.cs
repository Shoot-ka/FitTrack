using FitTrack.DTOs.Common;
using FitTrack.DTOs.Trainer;
using FitTrack.DTOs.Workouts;

namespace FitTrack.Services.Interfaces;

public interface ITrainerService
{
    Task<ClientDto> AddClientAsync(int trainerId, AddClientRequest request, CancellationToken ct);
    Task<PagedResult<ClientDto>> ListClientsAsync(int trainerId, int page, int pageSize, CancellationToken ct);
    Task<PagedResult<WorkoutDto>> ListClientWorkoutsAsync(
        int trainerId, int clientId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct);
}

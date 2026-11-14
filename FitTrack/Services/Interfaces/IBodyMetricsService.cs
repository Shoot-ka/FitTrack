using FitTrack.DTOs.Metrics;

namespace FitTrack.Services.Interfaces;

public interface IBodyMetricsService
{
    Task<BodyMetricsDto?> GetAsync(int userId, CancellationToken cancellationToken);
    Task<BodyMetricsDto> UpsertAsync(int userId, UpsertMetricsRequest request, CancellationToken cancellationToken);
}

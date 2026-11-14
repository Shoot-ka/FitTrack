using AutoMapper;
using FitTrack.Data;
using FitTrack.DTOs.Metrics;
using FitTrack.Model;
using FitTrack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FitTrack.Services;

public sealed class BodyMetricsService(AppDbContext db, IMapper mapper) : IBodyMetricsService
{
    public async Task<BodyMetricsDto?> GetAsync(int userId, CancellationToken cancellationToken)
    {
        var metrics = await db.BodyMetrics.AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        return metrics is null ? null : mapper.Map<BodyMetricsDto>(metrics);
    }

    public async Task<BodyMetricsDto> UpsertAsync(
        int userId, UpsertMetricsRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var metrics = await db.BodyMetrics.SingleOrDefaultAsync(
            item => item.UserId == userId, cancellationToken);

        if (metrics is null)
        {
            metrics = new BodyMetrics { UserId = userId };
            db.BodyMetrics.Add(metrics);
        }

        metrics.HeightCm = decimal.Round(request.HeightCm, 2, MidpointRounding.AwayFromZero);
        metrics.WeightKg = decimal.Round(request.WeightKg, 2, MidpointRounding.AwayFromZero);
        metrics.Goal = request.Goal!.Value;
        metrics.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return mapper.Map<BodyMetricsDto>(metrics);
    }
}

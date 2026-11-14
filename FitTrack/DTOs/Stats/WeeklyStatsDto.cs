namespace FitTrack.DTOs.Stats;

public sealed record WeeklyStatsDto(DateOnly WeekStart, int WorkoutsCount, decimal TotalVolume, int TotalMinutes);

using FitTrack.Enums;

namespace FitTrack.DTOs.Metrics;

public sealed record UpsertMetricsRequest(decimal HeightCm, decimal WeightKg, FitnessGoal? Goal);

using FitTrack.Enums;

namespace FitTrack.DTOs.Catalog;

public sealed record UpdateExerciseRequest(
    string? Name, string? Description, Equipment? Equipment, List<int>? MuscleGroupIds);

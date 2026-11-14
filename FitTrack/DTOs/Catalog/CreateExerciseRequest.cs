using FitTrack.Enums;

namespace FitTrack.DTOs.Catalog;

public sealed record CreateExerciseRequest(
    string? Name, string? Description, Equipment? Equipment, List<int>? MuscleGroupIds);

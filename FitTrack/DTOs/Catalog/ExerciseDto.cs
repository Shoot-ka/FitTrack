using FitTrack.Enums;

namespace FitTrack.DTOs.Catalog;

public sealed record ExerciseDto(
    int Id, string Name, string? Description, Equipment Equipment, IReadOnlyList<string> MuscleGroups);

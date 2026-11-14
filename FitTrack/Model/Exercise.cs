using FitTrack.Enums;

namespace FitTrack.Model;

public sealed class Exercise
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public Equipment Equipment { get; set; }
    public ICollection<MuscleGroup> MuscleGroups { get; } = new List<MuscleGroup>();
    public ICollection<WorkoutSet> WorkoutSets { get; } = new List<WorkoutSet>();
}

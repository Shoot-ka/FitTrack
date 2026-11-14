using System.ComponentModel.DataAnnotations.Schema;
using FitTrack.Enums;

namespace FitTrack.Model;

public sealed class Exercise
{
    public int Id { get; set; }
    [Column(TypeName = "nvarchar(80)")]
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public Equipment Equipment { get; set; }
    public ICollection<ExerciseMuscleGroup> ExerciseMuscleGroups { get; } = new List<ExerciseMuscleGroup>();
    public ICollection<WorkoutSet> WorkoutSets { get; } = new List<WorkoutSet>();
}

using System.ComponentModel.DataAnnotations.Schema;

namespace FitTrack.Model;

public sealed class MuscleGroup
{
    public int Id { get; set; }
    [Column(TypeName = "nvarchar(80)")]
    public string Name { get; set; } = "";
    public ICollection<ExerciseMuscleGroup> ExerciseMuscleGroups { get; } = new List<ExerciseMuscleGroup>();
}

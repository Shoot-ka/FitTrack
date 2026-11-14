using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Model;

[Table("ExerciseMuscleGroup")]
[PrimaryKey(nameof(ExerciseId), nameof(MuscleGroupId))]
public sealed class ExerciseMuscleGroup
{
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public int MuscleGroupId { get; set; }
    public MuscleGroup MuscleGroup { get; set; } = null!;
}

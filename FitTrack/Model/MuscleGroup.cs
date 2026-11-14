namespace FitTrack.Model;

public sealed class MuscleGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<Exercise> Exercises { get; } = new List<Exercise>();
}

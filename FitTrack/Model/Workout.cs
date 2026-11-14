namespace FitTrack.Model;

public sealed class Workout
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public int DurationMinutes { get; set; }
    public ICollection<WorkoutSet> Sets { get; } = new List<WorkoutSet>();
}

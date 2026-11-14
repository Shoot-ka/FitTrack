namespace FitTrack.DTOs.Workouts;

public sealed class WorkoutDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public int DurationMinutes { get; set; }
    public decimal TotalVolume { get; set; }
    public IReadOnlyList<WorkoutExerciseDto> Sets { get; set; } = [];
}

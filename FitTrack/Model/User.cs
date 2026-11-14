using FitTrack.Enums;

namespace FitTrack.Model;

public sealed class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public BodyMetrics? BodyMetrics { get; set; }
    public ICollection<Workout> Workouts { get; } = new List<Workout>();
    public ICollection<TrainerClient> Clients { get; } = new List<TrainerClient>();
    public ICollection<TrainerClient> Trainers { get; } = new List<TrainerClient>();
}

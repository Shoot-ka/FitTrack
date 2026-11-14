using System.ComponentModel.DataAnnotations.Schema;
using FitTrack.Enums;

namespace FitTrack.Model;

public sealed class User
{
    public int Id { get; set; }
    [Column(TypeName = "nvarchar(254)")]
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public BodyMetrics? BodyMetrics { get; set; }
    public ICollection<Workout> Workouts { get; } = new List<Workout>();
    [InverseProperty(nameof(TrainerClient.Trainer))]
    public ICollection<TrainerClient> Clients { get; } = new List<TrainerClient>();
    [InverseProperty(nameof(TrainerClient.Client))]
    public ICollection<TrainerClient> Trainers { get; } = new List<TrainerClient>();
}

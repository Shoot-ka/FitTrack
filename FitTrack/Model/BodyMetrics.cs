using Microsoft.EntityFrameworkCore;
using FitTrack.Enums;

namespace FitTrack.Model;

public sealed class BodyMetrics
{
    public int Id { get; set; }
    [Precision(5, 2)]
    public decimal HeightCm { get; set; }
    [Precision(5, 2)]
    public decimal WeightKg { get; set; }
    public FitnessGoal Goal { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

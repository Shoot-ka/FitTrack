using FitTrack.Enums;

namespace FitTrack.DTOs.Metrics;

public sealed class BodyMetricsDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal HeightCm { get; set; }
    public decimal WeightKg { get; set; }
    public FitnessGoal Goal { get; set; }
    public DateTime UpdatedAt { get; set; }
    public decimal Bmi { get; set; }
    public string BmiCategory { get; set; } = "";
}

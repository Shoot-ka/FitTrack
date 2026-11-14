namespace FitTrack.Model;

public sealed class TrainerClient
{
    public int TrainerId { get; set; }
    public User Trainer { get; set; } = null!;
    public int ClientId { get; set; }
    public User Client { get; set; } = null!;
    public DateTime Since { get; set; }
}

using Microsoft.EntityFrameworkCore;

namespace FitTrack.Model;

[PrimaryKey(nameof(TrainerId), nameof(ClientId))]
public sealed class TrainerClient
{
    public int TrainerId { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User Trainer { get; set; } = null!;
    public int ClientId { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User Client { get; set; } = null!;
    public DateTime Since { get; set; }
}

namespace FitTrack.DTOs.Trainer;

public sealed class ClientDto
{
    public int ClientId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime Since { get; set; }
    public DateTime? LastWorkoutDate { get; set; }
}

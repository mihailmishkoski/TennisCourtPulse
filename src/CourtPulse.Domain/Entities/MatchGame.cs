using CourtPulse.Domain.Enums;

namespace CourtPulse.Domain.Entities;

public sealed class MatchGame
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public int SetNumber { get; set; }
    public int GameNumber { get; set; }
    public PlayerSide PlayerServed { get; set; }
    public PlayerSide? ServeWinner { get; set; }
    public string? Score { get; set; }

    /// <summary>True once this game's completion has been folded into momentum.</summary>
    public bool MomentumProcessed { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<MatchPoint> Points { get; set; } = new List<MatchPoint>();
}

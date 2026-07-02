namespace CourtPulse.Domain.Entities;

public sealed class MatchSet
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public int SetNumber { get; set; }
    public int ScoreFirst { get; set; }
    public int ScoreSecond { get; set; }
}

namespace CourtPulse.Domain.Entities;

public sealed class PlayerMatchStatistic
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid PlayerId { get; set; }
    public string StatPeriod { get; set; } = "match";
    public string StatType { get; set; } = string.Empty;
    public string StatName { get; set; } = string.Empty;
    public string StatValue { get; set; } = string.Empty;
    public int? StatWon { get; set; }
    public int? StatTotal { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

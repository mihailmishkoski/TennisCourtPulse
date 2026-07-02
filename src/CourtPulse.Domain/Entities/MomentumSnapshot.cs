using CourtPulse.Domain.Enums;

namespace CourtPulse.Domain.Entities;

/// <summary>Append-only momentum reading used to render the graph over time.</summary>
public sealed class MomentumSnapshot
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public int SetNumber { get; set; }
    public int GameNumber { get; set; }
    public int PointNumber { get; set; }
    public PlayerSide Beneficiary { get; set; }
    public double Delta { get; set; }
    public string? Reason { get; set; }
    public double FirstCumulative { get; set; }
    public double SecondCumulative { get; set; }
    public double FirstEwma { get; set; }
    public double SecondEwma { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

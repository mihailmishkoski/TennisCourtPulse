namespace CourtPulse.Domain.Entities;

/// <summary>Append-only: points are only ever inserted, never updated.</summary>
public sealed class MatchPoint
{
    public Guid Id { get; set; }
    public Guid MatchGameId { get; set; }
    public int PointNumber { get; set; }

    /// <summary>Running game score at this point, e.g. "15 - 30".</summary>
    public string? Score { get; set; }

    public bool IsBreakPoint { get; set; }
    public bool IsSetPoint { get; set; }
    public bool IsMatchPoint { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

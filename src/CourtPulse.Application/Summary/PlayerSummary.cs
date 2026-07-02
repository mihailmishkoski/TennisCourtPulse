namespace CourtPulse.Application.Summary;

/// <summary>A single player's interpreted match report.</summary>
public sealed record PlayerSummary
{
    public required long PlayerKey { get; init; }
    public required IReadOnlyList<StatInsight> Strengths { get; init; }
    public required IReadOnlyList<StatInsight> Weaknesses { get; init; }
    public required IReadOnlyList<StatInsight> Highlights { get; init; }
}

/// <summary>
/// The full finished-match summary: both players' reports plus a few headline
/// takeaways that compare them directly. This is what a player (or coach) opens
/// after the match to see what worked and what to fix.
/// </summary>
public sealed record MatchSummary
{
    public required PlayerSummary First { get; init; }
    public required PlayerSummary Second { get; init; }

    /// <summary>Short cross-player takeaways, most significant first.</summary>
    public required IReadOnlyList<string> Headlines { get; init; }
}

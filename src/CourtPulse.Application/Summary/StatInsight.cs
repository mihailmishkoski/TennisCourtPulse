namespace CourtPulse.Application.Summary;

public enum InsightKind
{
    Strength = 1,
    Weakness = 2,
    Highlight = 3
}

/// <summary>
/// One interpreted observation about a player's match — the atom the Angular UI
/// renders as a "good side" / "weak side" / "did you know" chip. Carries the raw
/// numbers alongside the prose so the frontend can style or graph them.
/// </summary>
public sealed record StatInsight
{
    public required InsightKind Kind { get; init; }

    /// <summary>Friendly metric label, e.g. "First-serve points won".</summary>
    public required string Metric { get; init; }

    /// <summary>Ready-to-show sentence, e.g. "Dominant behind the first serve (81%).".</summary>
    public required string Summary { get; init; }

    /// <summary>This player's value on the metric (percentage or count).</summary>
    public double? PlayerValue { get; init; }

    /// <summary>Opponent's value on the same metric, when the insight is comparative.</summary>
    public double? OpponentValue { get; init; }

    /// <summary>
    /// Rough magnitude of the observation (edge in percentage points, or ratio
    /// distance from neutral). Used only to rank insights for display.
    /// </summary>
    public double Weight { get; init; }
}

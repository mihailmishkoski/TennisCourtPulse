namespace CourtPulse.Application.Summary;

/// <summary>
/// Tunable cut-offs for turning raw rates into "strength" / "weakness" verdicts.
/// Isolated here for the same reason as MomentumWeights — these are judgement
/// calls a coach might want to retune, not laws of the game.
/// </summary>
public sealed record SummaryThresholds
{
    /// <summary>At or above this rate (%) a metric reads as a clear strength.</summary>
    public double StrongPercentage { get; init; } = 65.0;

    /// <summary>At or below this rate (%) a metric reads as a clear weakness.</summary>
    public double WeakPercentage { get; init; } = 40.0;

    /// <summary>Head-to-head gap (percentage points) that counts as a decisive edge.</summary>
    public double DecisiveEdge { get; init; } = 12.0;

    /// <summary>
    /// Minimum attempts before a *pressure* rate (break points etc.) is trusted for
    /// a verdict. These are inherently rare, so the bar is low; below it we demote
    /// the observation to a Highlight rather than a Strength/Weakness.
    /// </summary>
    public int MinSampleForVerdict { get; init; } = 4;

    /// <summary>
    /// Minimum attempts before a *volume* rate (serve/return points, net play) is
    /// trusted for a verdict. Prevents an early-match "100% from 11 points" from
    /// being reported as a genuine strength — it becomes a Highlight instead.
    /// </summary>
    public int MinRateSample { get; init; } = 20;

    /// <summary>Winners-to-unforced-errors ratio at/above which shot-making is a strength.</summary>
    public double EfficientRatio { get; init; } = 1.2;

    /// <summary>Winners-to-unforced-errors ratio at/below which error count is a weakness.</summary>
    public double LooseRatio { get; init; } = 0.8;

    public static SummaryThresholds Default { get; } = new();
}

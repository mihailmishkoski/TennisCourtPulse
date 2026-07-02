namespace CourtPulse.Application.Analytics;

/// <summary>
/// All the tunable numbers behind momentum in one place. These are heuristics,
/// not physics — keeping them isolated here means the formula can be retuned
/// without touching the sync pipeline or the fold logic.
///
/// The default weights match the product spec; the smoothing factor and effort
/// weights are additions that make the "who's surging right now" and "who's
/// raising their level" read-outs behave sensibly. Treat every value as a knob.
/// </summary>
public sealed record MomentumWeights
{
    // --- Point-level signals (applied to the point's beneficiary) ---

    /// <summary>Won a point on your own serve (a routine hold-in-progress).</summary>
    public double HoldPoint { get; init; } = 1.0;

    /// <summary>Won a point on return — you are threatening the opponent's serve.</summary>
    public double ReturnPoint { get; init; } = 2.0;

    /// <summary>Server saved a break point (replaces the plain hold value on that point).</summary>
    public double BreakPointSaved { get; init; } = 2.0;

    /// <summary>Set point converted on the point that wins the set.</summary>
    public double SetPointConverted { get; init; } = 7.0;

    /// <summary>Match point converted. Beyond the base spec, but a natural apex signal.</summary>
    public double MatchPointConverted { get; init; } = 10.0;

    /// <summary>Double fault, applied to the server — only when the feed exposes it.</summary>
    public double DoubleFault { get; init; } = -2.0;

    // --- Game-boundary signal ---

    /// <summary>Broke the opponent's serve (awarded once, when the game closes).</summary>
    public double BreakConverted { get; init; } = 5.0;

    // --- Smoothing ---

    /// <summary>
    /// EWMA smoothing factor for the live "momentum meter" (0..1). Higher = more
    /// reactive to the last few points; lower = smoother. ~0.25 weights roughly
    /// the last 6–8 points meaningfully, which reads well as "who's hot".
    /// </summary>
    public double SmoothingAlpha { get; init; } = 0.25;

    // --- Effort / "trying harder" blend (weights sum is not required to be 1) ---

    public double EffortSlopeWeight { get; init; } = 0.5;
    public double EffortClutchWeight { get; init; } = 0.3;
    public double EffortReturnWeight { get; init; } = 0.2;

    /// <summary>
    /// Minimum gap between the two players' effort indices before we call a
    /// leader; inside this band the match is "even" rather than noisily flipping.
    /// </summary>
    public double EffortLeaderMargin { get; init; } = 0.08;

    public static MomentumWeights Default { get; } = new();
}

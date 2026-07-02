namespace CourtPulse.Application.Analytics;

/// <summary>
/// The carry-forward state of the momentum fold. This is what makes live,
/// incremental calculation possible: persist (or cache) this after each cycle
/// and feed it back next cycle together with only the newly-completed games,
/// instead of recomputing the whole match history every 25 seconds.
///
/// Two distinct notions of momentum are tracked, because they answer different
/// questions and the frontend wants both:
///   * Cumulative — the running weighted advantage since the first point. This
///     is the momentum *graph* (how we got here).
///   * Ewma — an exponentially-weighted recent average. This is the live
///     momentum *meter* (who is surging right now).
/// </summary>
public sealed record MomentumState
{
    public double FirstCumulative { get; init; }
    public double SecondCumulative { get; init; }

    public double FirstEwma { get; init; }
    public double SecondEwma { get; init; }

    /// <summary>Cumulative advantage of First over Second (negative = Second ahead).</summary>
    public double CumulativeDifferential => FirstCumulative - SecondCumulative;

    /// <summary>Instantaneous surge of First over Second (negative = Second surging).</summary>
    public double EwmaDifferential => FirstEwma - SecondEwma;

    /// <summary>The starting state for a match with no points yet processed.</summary>
    public static MomentumState Empty { get; } = new();
}

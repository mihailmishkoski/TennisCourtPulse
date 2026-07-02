namespace CourtPulse.Application.Analytics;

/// <summary>
/// Result of an incremental advance: the snapshots produced for the points that
/// were newly folded in, and the carry-forward <see cref="MomentumState"/> to
/// persist for next cycle.
/// </summary>
public sealed record MomentumProgression
{
    public required IReadOnlyList<MomentumSnapshotResult> Snapshots { get; init; }
    public required MomentumState State { get; init; }
}

/// <summary>
/// Pure momentum / effort engine. No EF Core, no HTTP, no time — everything it
/// needs is passed in, so it can be exhaustively unit-tested and reasoned about.
/// </summary>
public interface IMomentumCalculationService
{
    /// <summary>
    /// Full recompute over an entire ordered timeline. Use on match load, in
    /// tests, or as a periodic reconciliation guard. Games are assumed sorted by
    /// (set, game) and points by point number.
    /// </summary>
    IReadOnlyList<MomentumSnapshotResult> ComputeTimeline(IReadOnlyList<GameInput> orderedGames);

    /// <summary>
    /// Incremental advance for the live loop. Feed the carried state plus only
    /// the games that have *completed* since last cycle (buffer the in-progress
    /// game until it closes, so break/hold bonuses resolve exactly once). Ordering
    /// is the caller's responsibility and is critical — momentum is path-dependent.
    /// </summary>
    MomentumProgression Advance(MomentumState priorState, IReadOnlyList<GameInput> newlyCompletedGames);

    /// <summary>
    /// Evaluate "who is raising their level" over the last <paramref name="window"/>
    /// points of the supplied snapshot sequence.
    /// </summary>
    MatchEffort EvaluateEffort(IReadOnlyList<MomentumSnapshotResult> snapshots, int window);
}

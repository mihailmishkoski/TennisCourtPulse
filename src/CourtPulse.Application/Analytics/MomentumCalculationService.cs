using CourtPulse.Domain.Enums;

namespace CourtPulse.Application.Analytics;

/// <summary>
/// Pure implementation of the momentum / effort heuristics. It is a deterministic
/// fold over the ordered timeline — no I/O, no clock, no randomness — so its
/// behaviour is fully pinned down by its inputs and it can be unit-tested to the
/// point value. See <see cref="MomentumWeights"/> for every tunable number and
/// <see cref="PointInput"/> for the (deliberately narrow) set of signals we trust
/// the api-tennis feed to actually provide.
/// </summary>
public sealed class MomentumCalculationService : IMomentumCalculationService
{
    private readonly MomentumWeights _weights;

    public MomentumCalculationService(MomentumWeights? weights = null)
    {
        _weights = weights ?? MomentumWeights.Default;
    }

    public IReadOnlyList<MomentumSnapshotResult> ComputeTimeline(IReadOnlyList<GameInput> orderedGames)
    {
        MomentumProgression progression = Fold(MomentumState.Empty, orderedGames);
        return progression.Snapshots;
    }

    public MomentumProgression Advance(MomentumState priorState, IReadOnlyList<GameInput> newlyCompletedGames)
    {
        return Fold(priorState, newlyCompletedGames);
    }

    /// <summary>
    /// The single shared fold used by both the full recompute and the live
    /// advance. Path-dependent by construction: each point is applied to the
    /// running state in order, and a break-of-serve bonus is merged into the
    /// game-deciding point when a completed game changed hands.
    /// </summary>
    private MomentumProgression Fold(MomentumState startState, IReadOnlyList<GameInput> games)
    {
        List<MomentumSnapshotResult> snapshots = new List<MomentumSnapshotResult>();
        MomentumState state = startState;

        foreach (GameInput game in games)
        {
            for (int i = 0; i < game.Points.Count; i++)
            {
                PointInput point = game.Points[i];
                PointEffect effect = Evaluate(point);

                state = Apply(state, effect.Beneficiary, effect.Delta);

                snapshots.Add(new MomentumSnapshotResult
                {
                    SetNumber = point.SetNumber,
                    GameNumber = point.GameNumber,
                    PointNumber = point.PointNumber,
                    Beneficiary = effect.Beneficiary,
                    Delta = effect.Delta,
                    Reason = effect.Reason,
                    WasReturnPoint = effect.WasReturnPoint,
                    WasPressurePoint = effect.WasPressurePoint,
                    State = state
                });
            }

            // Break-of-serve bonus is applied from the reliable game outcome, NOT from
            // the point stream — the feed frequently omits the game-deciding point (and
            // sometimes every point), but serve_winner/serve_lost are always present.
            // Emitting it as its own snapshot means a break still registers on the
            // momentum graph even for a game we received no point detail for.
            if (game.IsComplete && game.ServeWinner != game.Server)
            {
                PlayerSide breaker = game.ServeWinner!.Value;
                state = Apply(state, breaker, _weights.BreakConverted);

                snapshots.Add(new MomentumSnapshotResult
                {
                    SetNumber = game.SetNumber,
                    GameNumber = game.GameNumber,
                    // Sort after this game's real points; append-only, no uniqueness needed.
                    PointNumber = game.Points.Count + 1,
                    Beneficiary = breaker,
                    Delta = _weights.BreakConverted,
                    Reason = "Break of serve",
                    WasReturnPoint = true,
                    WasPressurePoint = true,
                    State = state
                });
            }
        }

        return new MomentumProgression { Snapshots = snapshots, State = state };
    }

    /// <summary>
    /// Classify a single point into a signed momentum effect. Precedence runs
    /// from the most decisive signal down to routine play; only one branch fires
    /// so signals never double-count on the same point.
    /// </summary>
    private PointEffect Evaluate(PointInput point)
    {
        bool wonOnServe = point.Winner == point.Server;
        bool pressure = point.IsBreakPoint || point.IsSetPoint || point.IsMatchPoint;

        // A double fault hands the point to the returner for free. We frame it as
        // the returner's gain (equivalent, in differential terms, to docking the
        // server) and only ever reach here when the feed actually flagged it.
        if (point.IsDoubleFault)
        {
            return new PointEffect(Opponent(point.Server), Math.Abs(_weights.DoubleFault),
                "Double fault by server", true, pressure);
        }

        if (point.IsMatchPoint)
        {
            return new PointEffect(point.Winner, _weights.MatchPointConverted,
                "Match point won", !wonOnServe, true);
        }

        if (point.IsSetPoint)
        {
            return new PointEffect(point.Winner, _weights.SetPointConverted,
                "Set point won", !wonOnServe, true);
        }

        if (point.IsBreakPoint)
        {
            if (wonOnServe)
            {
                return new PointEffect(point.Server, _weights.BreakPointSaved,
                    "Break point saved", false, true);
            }

            return new PointEffect(point.Winner, _weights.ReturnPoint,
                "Break point won on return", true, true);
        }

        if (wonOnServe)
        {
            return new PointEffect(point.Winner, _weights.HoldPoint,
                "Point held on serve", false, false);
        }

        return new PointEffect(point.Winner, _weights.ReturnPoint,
            "Point won on return", true, false);
    }

    /// <summary>
    /// Fold one point's effect into the state: the beneficiary gains the delta on
    /// the cumulative line, and the EWMA meter is nudged toward the player who
    /// just scored (the other player's EWMA simply decays this step).
    /// </summary>
    private MomentumState Apply(MomentumState state, PlayerSide beneficiary, double delta)
    {
        double alpha = _weights.SmoothingAlpha;
        bool first = beneficiary == PlayerSide.First;

        double firstSample = first ? delta : 0.0;
        double secondSample = first ? 0.0 : delta;

        return state with
        {
            FirstCumulative = state.FirstCumulative + firstSample,
            SecondCumulative = state.SecondCumulative + secondSample,
            FirstEwma = (alpha * firstSample) + ((1.0 - alpha) * state.FirstEwma),
            SecondEwma = (alpha * secondSample) + ((1.0 - alpha) * state.SecondEwma)
        };
    }

    public MatchEffort EvaluateEffort(IReadOnlyList<MomentumSnapshotResult> snapshots, int window)
    {
        int count = snapshots.Count;
        int size = Math.Min(window, count);

        if (size <= 0)
        {
            return new MatchEffort { FirstIndex = 0.5, SecondIndex = 0.5, Leader = null, WindowSize = 0 };
        }

        int startIndex = count - size;
        MomentumState baseline = startIndex > 0 ? snapshots[startIndex - 1].State : MomentumState.Empty;
        MomentumState latest = snapshots[count - 1].State;

        // Slope: share of the momentum *gained* over the window (recent trend, not
        // total score) — who has been pulling ahead lately.
        double firstGain = latest.FirstCumulative - baseline.FirstCumulative;
        double secondGain = latest.SecondCumulative - baseline.SecondCumulative;
        double firstSlope = Share(firstGain, secondGain);

        int firstPressureWins = 0;
        int secondPressureWins = 0;
        int firstReturnWins = 0;
        int secondReturnWins = 0;

        for (int i = startIndex; i < count; i++)
        {
            MomentumSnapshotResult snapshot = snapshots[i];
            bool first = snapshot.Beneficiary == PlayerSide.First;

            if (snapshot.WasPressurePoint)
            {
                if (first) { firstPressureWins++; } else { secondPressureWins++; }
            }

            if (snapshot.WasReturnPoint)
            {
                if (first) { firstReturnWins++; } else { secondReturnWins++; }
            }
        }

        double firstClutch = Share(firstPressureWins, secondPressureWins);
        double firstReturn = Share(firstReturnWins, secondReturnWins);

        double firstIndex =
            (_weights.EffortSlopeWeight * firstSlope) +
            (_weights.EffortClutchWeight * firstClutch) +
            (_weights.EffortReturnWeight * firstReturn);

        double weightSum = _weights.EffortSlopeWeight + _weights.EffortClutchWeight + _weights.EffortReturnWeight;
        double secondIndex = weightSum - firstIndex;

        PlayerSide? leader;
        double gap = firstIndex - secondIndex;
        if (gap > _weights.EffortLeaderMargin) { leader = PlayerSide.First; }
        else if (gap < -_weights.EffortLeaderMargin) { leader = PlayerSide.Second; }
        else { leader = null; }

        return new MatchEffort
        {
            FirstIndex = firstIndex,
            SecondIndex = secondIndex,
            Leader = leader,
            WindowSize = size
        };
    }

    /// <summary>
    /// First player's share of a non-negative pair, defaulting to an even 0.5
    /// when there is nothing to split (no pressure points yet, no net gain, etc.).
    /// </summary>
    private static double Share(double firstValue, double secondValue)
    {
        double total = firstValue + secondValue;
        if (total <= 0.0)
        {
            return 0.5;
        }

        return firstValue / total;
    }

    private static PlayerSide Opponent(PlayerSide side)
    {
        return side == PlayerSide.First ? PlayerSide.Second : PlayerSide.First;
    }

    /// <summary>Internal carrier for a classified point, before it hits the running state.</summary>
    private readonly record struct PointEffect(
        PlayerSide Beneficiary,
        double Delta,
        string Reason,
        bool WasReturnPoint,
        bool WasPressurePoint);
}

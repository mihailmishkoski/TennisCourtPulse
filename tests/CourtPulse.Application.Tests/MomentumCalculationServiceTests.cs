using CourtPulse.Application.Analytics;
using CourtPulse.Domain.Enums;
using Xunit;

namespace CourtPulse.Application.Tests;

/// <summary>
/// Behavioural tests pinning the momentum heuristics. These double as executable
/// documentation of what each signal is worth and how the two momentum notions
/// (cumulative graph vs. EWMA meter) differ.
/// </summary>
public sealed class MomentumCalculationServiceTests
{
    private readonly MomentumCalculationService _service = new MomentumCalculationService();

    private static PointInput Point(int set, int game, int number, PlayerSide server, PlayerSide winner,
        bool breakPoint = false, bool setPoint = false, bool matchPoint = false, bool doubleFault = false)
    {
        return new PointInput
        {
            SetNumber = set,
            GameNumber = game,
            PointNumber = number,
            Server = server,
            Winner = winner,
            IsBreakPoint = breakPoint,
            IsSetPoint = setPoint,
            IsMatchPoint = matchPoint,
            IsDoubleFault = doubleFault
        };
    }

    [Fact]
    public void HoldPoint_IsWorthLessThanReturnPoint()
    {
        GameInput hold = new GameInput
        {
            SetNumber = 1, GameNumber = 1, Server = PlayerSide.First, ServeWinner = PlayerSide.First,
            Points = new[] { Point(1, 1, 1, PlayerSide.First, PlayerSide.First) }
        };
        GameInput returnWin = new GameInput
        {
            SetNumber = 1, GameNumber = 1, Server = PlayerSide.First, ServeWinner = null,
            Points = new[] { Point(1, 1, 1, PlayerSide.First, PlayerSide.Second) }
        };

        double holdDelta = _service.ComputeTimeline(new[] { hold })[0].Delta;
        double returnDelta = _service.ComputeTimeline(new[] { returnWin })[0].Delta;

        Assert.Equal(1.0, holdDelta);
        Assert.Equal(2.0, returnDelta);
        Assert.True(returnDelta > holdDelta);
    }

    [Fact]
    public void BreakOfServe_EmitsAGameLevelBonusSnapshot_FromTheReliableGameOutcome()
    {
        // Second player breaks First's serve: wins the game-deciding point on return.
        GameInput brokenGame = new GameInput
        {
            SetNumber = 1, GameNumber = 1, Server = PlayerSide.First, ServeWinner = PlayerSide.Second,
            Points = new[]
            {
                Point(1, 1, 1, PlayerSide.First, PlayerSide.Second),
                Point(1, 1, 2, PlayerSide.First, PlayerSide.Second, breakPoint: true)
            }
        };

        IReadOnlyList<MomentumSnapshotResult> snapshots = _service.ComputeTimeline(new[] { brokenGame });
        MomentumSnapshotResult breakBonus = snapshots[^1];

        // Two return points (+2 each) then a standalone break-of-serve snapshot (+5).
        Assert.Equal("Break of serve", breakBonus.Reason);
        Assert.Equal(PlayerSide.Second, breakBonus.Beneficiary);
        Assert.Equal(5.0, breakBonus.Delta);
        Assert.Equal(9.0, breakBonus.State.SecondCumulative); // 2 + 2 + 5
    }

    [Fact]
    public void BreakOfServe_RegistersEvenWhenTheFeedGivesNoPointsForTheGame()
    {
        // 49/230 games in the real feed carry a serve_winner but no points at all.
        GameInput pointlessBreak = new GameInput
        {
            SetNumber = 1, GameNumber = 1, Server = PlayerSide.First, ServeWinner = PlayerSide.Second,
            Points = Array.Empty<PointInput>()
        };

        IReadOnlyList<MomentumSnapshotResult> snapshots = _service.ComputeTimeline(new[] { pointlessBreak });

        Assert.Single(snapshots);
        Assert.Equal("Break of serve", snapshots[0].Reason);
        Assert.Equal(5.0, snapshots[0].State.SecondCumulative);
    }

    [Fact]
    public void Ewma_TracksRecentSurge_WhileCumulativeTracksWholeMatch()
    {
        // First banks a big early lead (8 return points), then Second wins the last
        // 6 points — not enough to overtake the cumulative total, but enough to own
        // the recent-form meter. Both games left in-progress so no break bonus muddies
        // the arithmetic; we're isolating cumulative-vs-EWMA here.
        PointInput[] firstRun = new PointInput[8];
        for (int i = 0; i < 8; i++) { firstRun[i] = Point(1, 1, i + 1, PlayerSide.Second, PlayerSide.First); }

        PointInput[] secondRun = new PointInput[6];
        for (int i = 0; i < 6; i++) { secondRun[i] = Point(1, 2, i + 1, PlayerSide.First, PlayerSide.Second); }

        List<GameInput> games = new List<GameInput>
        {
            new GameInput { SetNumber = 1, GameNumber = 1, Server = PlayerSide.Second, ServeWinner = null, Points = firstRun },
            new GameInput { SetNumber = 1, GameNumber = 2, Server = PlayerSide.First, ServeWinner = null, Points = secondRun }
        };

        MomentumState final = _service.ComputeTimeline(games)[^1].State;

        // Cumulative still favours First (won 4 return points early = +8 vs Second's break game).
        Assert.True(final.CumulativeDifferential > 0, "First should lead the cumulative graph");
        // But the live meter has swung to Second, who won the most recent points.
        Assert.True(final.EwmaDifferential < 0, "Second should own the current momentum meter");
    }

    [Fact]
    public void Advance_FromCarriedState_MatchesFullRecompute()
    {
        GameInput first = new GameInput { SetNumber = 1, GameNumber = 1, Server = PlayerSide.First, ServeWinner = PlayerSide.First,
            Points = new[] { Point(1, 1, 1, PlayerSide.First, PlayerSide.First), Point(1, 1, 2, PlayerSide.First, PlayerSide.First) } };
        GameInput second = new GameInput { SetNumber = 1, GameNumber = 2, Server = PlayerSide.Second, ServeWinner = PlayerSide.First,
            Points = new[] { Point(1, 2, 1, PlayerSide.Second, PlayerSide.First, breakPoint: true) } };

        // Full recompute of both games.
        MomentumState full = _service.ComputeTimeline(new[] { first, second })[^1].State;

        // Incremental: process game 1, carry state, then process game 2.
        MomentumProgression step1 = _service.Advance(MomentumState.Empty, new[] { first });
        MomentumProgression step2 = _service.Advance(step1.State, new[] { second });

        Assert.Equal(full.FirstCumulative, step2.State.FirstCumulative, 6);
        Assert.Equal(full.SecondCumulative, step2.State.SecondCumulative, 6);
        Assert.Equal(full.FirstEwma, step2.State.FirstEwma, 6);
    }

    [Fact]
    public void EvaluateEffort_CreditsThePlayerWinningRecentPressureAndReturnPoints()
    {
        // Second player claws back the last game with return + break points under pressure.
        List<GameInput> games = new List<GameInput>
        {
            new GameInput { SetNumber = 1, GameNumber = 1, Server = PlayerSide.First, ServeWinner = PlayerSide.First,
                Points = new[] { Point(1, 1, 1, PlayerSide.First, PlayerSide.First), Point(1, 1, 2, PlayerSide.First, PlayerSide.First) } },
            new GameInput { SetNumber = 1, GameNumber = 2, Server = PlayerSide.First, ServeWinner = PlayerSide.Second,
                Points = new[]
                {
                    Point(1, 2, 1, PlayerSide.First, PlayerSide.Second),
                    Point(1, 2, 2, PlayerSide.First, PlayerSide.Second, breakPoint: true)
                } }
        };

        IReadOnlyList<MomentumSnapshotResult> snapshots = _service.ComputeTimeline(games);
        MatchEffort effort = _service.EvaluateEffort(snapshots, window: 2);

        Assert.Equal(PlayerSide.Second, effort.Leader);
        Assert.True(effort.SecondIndex > effort.FirstIndex);
        Assert.Equal(2, effort.WindowSize);
    }

    [Fact]
    public void DoubleFault_CreditsTheReturnerAndOnlyWhenFlagged()
    {
        GameInput game = new GameInput { SetNumber = 1, GameNumber = 1, Server = PlayerSide.First, ServeWinner = null,
            Points = new[] { Point(1, 1, 1, PlayerSide.First, PlayerSide.Second, doubleFault: true) } };

        MomentumSnapshotResult snapshot = _service.ComputeTimeline(new[] { game })[0];

        Assert.Equal(PlayerSide.Second, snapshot.Beneficiary);
        Assert.Equal(2.0, snapshot.Delta);
        Assert.Contains("Double fault", snapshot.Reason);
    }
}

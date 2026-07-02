using CourtPulse.Application.Analytics;
using CourtPulse.Domain.Enums;
using Xunit;

namespace CourtPulse.Application.Tests;

public sealed class TurningPointDetectorTests
{
    private readonly MomentumCalculationService _momentum = new MomentumCalculationService();
    private readonly TurningPointDetector _detector = new TurningPointDetector();

    private static PointInput P(int game, int number, PlayerSide server, PlayerSide winner)
    {
        return new PointInput { SetNumber = 1, GameNumber = game, PointNumber = number, Server = server, Winner = winner };
    }

    [Fact]
    public void DetectsALeadChange_WhenTheOverallDifferentialFlips()
    {
        // First edges ahead, then Second overtakes with a break.
        List<GameInput> games = new List<GameInput>
        {
            new GameInput { SetNumber = 1, GameNumber = 1, Server = PlayerSide.Second, ServeWinner = PlayerSide.First,
                Points = new[] { P(1, 1, PlayerSide.Second, PlayerSide.First) } },
            new GameInput { SetNumber = 1, GameNumber = 2, Server = PlayerSide.First, ServeWinner = PlayerSide.Second,
                Points = new[] { P(2, 1, PlayerSide.First, PlayerSide.Second), P(2, 2, PlayerSide.First, PlayerSide.Second) } }
        };

        IReadOnlyList<MomentumSnapshotResult> snapshots = _momentum.ComputeTimeline(games);
        IReadOnlyList<TurningPoint> turning = _detector.Detect(snapshots, maxCount: 5);

        Assert.Contains(turning, t => t.LeadChanged);
        Assert.Contains(turning, t => t.Reason.Contains("took the lead"));
    }

    [Fact]
    public void RespectsMaxCount_AndReturnsChronologicalOrder()
    {
        List<GameInput> games = new List<GameInput>();
        for (int g = 1; g <= 8; g++)
        {
            PlayerSide server = g % 2 == 1 ? PlayerSide.First : PlayerSide.Second;
            PlayerSide winner = g % 3 == 0 ? PlayerSide.Second : PlayerSide.First;
            games.Add(new GameInput
            {
                SetNumber = 1, GameNumber = g, Server = server, ServeWinner = winner,
                Points = new[] { P(g, 1, server, winner) }
            });
        }

        IReadOnlyList<MomentumSnapshotResult> snapshots = _momentum.ComputeTimeline(games);
        IReadOnlyList<TurningPoint> turning = _detector.Detect(snapshots, maxCount: 3);

        Assert.True(turning.Count <= 3);
        for (int i = 1; i < turning.Count; i++)
        {
            Assert.True(turning[i].GameNumber >= turning[i - 1].GameNumber);
        }
    }
}

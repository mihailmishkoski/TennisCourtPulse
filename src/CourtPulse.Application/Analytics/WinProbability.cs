using CourtPulse.Domain.Enums;

namespace CourtPulse.Application.Analytics;

/// <summary>Current match state needed to estimate a live win probability.</summary>
public sealed record WinProbabilityInput
{
    public int FirstSetsWon { get; init; }
    public int SecondSetsWon { get; init; }

    /// <summary>Games each player has in the set currently in progress.</summary>
    public int FirstGamesInSet { get; init; }
    public int SecondGamesInSet { get; init; }

    /// <summary>Points in the current game, relative to whoever is serving (0,1,2,3,4=adv).</summary>
    public int ServerPointsInGame { get; init; }
    public int ReturnerPointsInGame { get; init; }

    public required PlayerSide Serving { get; init; }

    /// <summary>Best-of-five (Grand Slam men's) vs best-of-three.</summary>
    public bool BestOfFive { get; init; }

    /// <summary>Probability the First player wins a point on the First player's serve.</summary>
    public double FirstServePointWin { get; init; } = 0.62;

    /// <summary>Probability the Second player wins a point on the Second player's serve.</summary>
    public double SecondServePointWin { get; init; } = 0.62;
}

/// <summary>Live win probability for each side (values sum to 1).</summary>
public sealed record WinProbability
{
    public required double First { get; init; }
    public required double Second { get; init; }
}

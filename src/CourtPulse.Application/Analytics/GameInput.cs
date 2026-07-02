using CourtPulse.Domain.Enums;

namespace CourtPulse.Application.Analytics;

/// <summary>
/// One game's worth of the timeline: the server, the (optional) resolved game
/// winner, and its ordered points. Grouping by game matters because several
/// momentum signals — most importantly a *break of serve* — can only be
/// resolved at the game boundary, not from a single point in isolation.
/// </summary>
public sealed record GameInput
{
    public required int SetNumber { get; init; }

    public required int GameNumber { get; init; }

    public required PlayerSide Server { get; init; }

    /// <summary>
    /// Who won the game, once it has finished. Null while the game is still in
    /// progress. When <see cref="ServeWinner"/> differs from <see cref="Server"/>
    /// the game was a break of serve.
    /// </summary>
    public PlayerSide? ServeWinner { get; init; }

    /// <summary>Points in play order. The engine assumes this is already sorted.</summary>
    public required IReadOnlyList<PointInput> Points { get; init; }

    /// <summary>A game is only "complete" once its winner is known.</summary>
    public bool IsComplete => ServeWinner.HasValue;
}

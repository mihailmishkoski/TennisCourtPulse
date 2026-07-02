using CourtPulse.Domain.Enums;

namespace CourtPulse.Application.Analytics;

/// <summary>
/// The "who's trying harder / raising their level right now" read-out. This is
/// intentionally a derived, opinionated blend rather than a raw feed value —
/// "effort" is not something the API reports, so we operationalise it from the
/// signals that actually correlate with a player stepping up over a recent
/// window of points:
///   * Slope   — how fast they're gaining cumulative momentum lately.
///   * Clutch  — win rate on the pressure points (break/set/match points).
///   * Return  — how often they're winning points on the opponent's serve.
///
/// A player who is grinding out return points and saving/taking pressure points
/// is, in tennis terms, the one lifting their level — regardless of the score.
/// </summary>
public sealed record MatchEffort
{
    public required double FirstIndex { get; init; }
    public required double SecondIndex { get; init; }

    /// <summary>
    /// Who is raising their level, or null when the two are within the
    /// configured margin (a genuine "even" moment rather than a coin-flip).
    /// </summary>
    public required PlayerSide? Leader { get; init; }

    /// <summary>Number of points the window was actually computed over.</summary>
    public required int WindowSize { get; init; }
}

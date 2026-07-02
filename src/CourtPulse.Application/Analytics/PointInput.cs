using CourtPulse.Domain.Enums;

namespace CourtPulse.Application.Analytics;

/// <summary>
/// One resolved point in the match timeline, already normalised out of the raw
/// api-tennis payload. This type is deliberately POCO and free of EF Core /
/// HTTP concerns so the calculation engine stays pure and unit-testable.
///
/// IMPORTANT — what the payload actually gives us:
/// api-tennis' <c>pointbypoint</c> array reports, per game, the serving player
/// and, per point, a running game score string (e.g. "40 - 30") plus optional
/// break_point / set_point / match_point markers. It does NOT reliably tell you
/// *how* a point was won (winner, ace, double fault). We therefore derive the
/// point <see cref="Winner"/> from the change in the game score string, and we
/// only set <see cref="IsDoubleFault"/> when a payload actually exposes it.
/// Do not fabricate signals the feed doesn't contain.
/// </summary>
public sealed record PointInput
{
    /// <summary>1-based set index this point belongs to.</summary>
    public required int SetNumber { get; init; }

    /// <summary>1-based game index within the set.</summary>
    public required int GameNumber { get; init; }

    /// <summary>1-based point index within the game (ordering key — must be monotonic).</summary>
    public required int PointNumber { get; init; }

    /// <summary>Running game score at this point as delivered, e.g. "15 - 30".</summary>
    public string? Score { get; init; }

    /// <summary>Which side was serving this point.</summary>
    public required PlayerSide Server { get; init; }

    /// <summary>Which side won this point (derived from the game-score delta).</summary>
    public required PlayerSide Winner { get; init; }

    /// <summary>True when the returner held a break point on this point.</summary>
    public bool IsBreakPoint { get; init; }

    /// <summary>True when this point could close out a set.</summary>
    public bool IsSetPoint { get; init; }

    /// <summary>True when this point could close out the match.</summary>
    public bool IsMatchPoint { get; init; }

    /// <summary>
    /// True only when the feed explicitly attributes a double fault to the
    /// server on this point. Left false when unknowable — see type remarks.
    /// </summary>
    public bool IsDoubleFault { get; init; }
}

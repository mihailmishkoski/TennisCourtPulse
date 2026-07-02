using CourtPulse.Application.Analytics;
using CourtPulse.Application.Summary;
using CourtPulse.Domain.Enums;

namespace CourtPulse.Application.Mapping;

/// <summary>
/// A single api-tennis match normalised into the shapes our engines consume.
/// This is the clean boundary between the messy external feed and everything
/// downstream (momentum, summary, persistence).
/// </summary>
public sealed record MappedMatch
{
    public required long EventKey { get; init; }
    public required int TournamentKey { get; init; }
    public string? TournamentName { get; init; }
    public string? Round { get; init; }
    public string? EventType { get; init; }

    public required long FirstPlayerKey { get; init; }
    public required string FirstPlayerName { get; init; }
    public string? FirstPlayerLogo { get; init; }
    public required long SecondPlayerKey { get; init; }
    public required string SecondPlayerName { get; init; }
    public string? SecondPlayerLogo { get; init; }

    /// <summary>True when both participants are doubles pairs ("A/ B").</summary>
    public bool IsDoubles { get; init; }

    public string? Status { get; init; }
    public string? FinalResult { get; init; }

    /// <summary>
    /// Finished ⇔ status is "Finished" OR a winner is resolved — a match can be
    /// finished while still appearing in the live feed.
    /// </summary>
    public required bool IsFinished { get; init; }

    /// <summary>Currently serving side, when the feed reports it (often null).</summary>
    public PlayerSide? Serving { get; init; }

    /// <summary>Live game score as tennis-point indices (0,1,2,3,4=advantage), First player.</summary>
    public int CurrentGameFirstPoints { get; init; }

    /// <summary>Live game score as tennis-point indices, Second player.</summary>
    public int CurrentGameSecondPoints { get; init; }

    /// <summary>Resolved winner side, when known.</summary>
    public PlayerSide? Winner { get; init; }

    /// <summary>Set scores as (first, second) pairs in set order.</summary>
    public required IReadOnlyList<(int First, int Second)> SetScores { get; init; }

    /// <summary>Ordered game timeline for the momentum engine.</summary>
    public required IReadOnlyList<GameInput> Games { get; init; }

    /// <summary>Statistic rows for the summary engine.</summary>
    public required IReadOnlyList<MatchStatLine> Stats { get; init; }
}

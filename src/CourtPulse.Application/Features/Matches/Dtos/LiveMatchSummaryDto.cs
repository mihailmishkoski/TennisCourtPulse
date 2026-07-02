namespace CourtPulse.Application.Features.Matches.Dtos;

public sealed record LiveMatchSummaryDto
{
    public required Guid Id { get; init; }
    public required string Tournament { get; init; }
    public required string FirstPlayer { get; init; }
    public string? FirstPlayerLogo { get; init; }
    public required string SecondPlayer { get; init; }
    public string? SecondPlayerLogo { get; init; }
    public string? Status { get; init; }
    public required bool IsLive { get; init; }
    public required bool IsFinished { get; init; }
    public string? FinalResult { get; init; }

    /// <summary>In-progress game score, e.g. "15 - 0"; null when the match is not live.</summary>
    public string? CurrentGameScore { get; init; }

    /// <summary>Which side is serving ("First"/"Second"), when the feed reports it.</summary>
    public string? Serving { get; init; }

    /// <summary>Grouping for the live list: "Men" / "Women" / "Other".</summary>
    public required string Gender { get; init; }

    /// <summary>Tour tag derived from the event type: ATP / WTA / ITF / Challenger, or null.</summary>
    public string? Tour { get; init; }

    /// <summary>Per-set games score, ordered by set number — for the compact list rows.</summary>
    public required IReadOnlyList<SetScoreDto> Sets { get; init; }

    /// <summary>Cumulative momentum lead (First − Second); the live-list micro-indicator.</summary>
    public required double MomentumDifferential { get; init; }
}

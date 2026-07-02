namespace CourtPulse.Application.Features.Matches.Dtos;

public sealed record SetScoreDto(int SetNumber, int First, int Second);

public sealed record MatchDetailDto
{
    public required Guid Id { get; init; }
    public required string Tournament { get; init; }
    public string? Round { get; init; }
    public string? EventType { get; init; }
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

    public string? Winner { get; init; }
    public required IReadOnlyList<SetScoreDto> Sets { get; init; }
}

namespace CourtPulse.Application.Features.Matches.Dtos;

/// <summary>A single point with its running score (e.g. "15 - 30") and pressure flags.</summary>
public sealed record TimelinePointDto(
    int PointNumber,
    string? Score,
    bool IsBreakPoint,
    bool IsSetPoint,
    bool IsMatchPoint);

public sealed record TimelineGameDto
{
    public required int GameNumber { get; init; }
    public required string Server { get; init; }
    public string? ServeWinner { get; init; }
    public required IReadOnlyList<TimelinePointDto> Points { get; init; }
}

public sealed record TimelineSetDto
{
    public required int SetNumber { get; init; }
    public required IReadOnlyList<TimelineGameDto> Games { get; init; }
}

public sealed record MatchTimelineDto
{
    public required Guid MatchId { get; init; }
    public required IReadOnlyList<TimelineSetDto> Sets { get; init; }
}

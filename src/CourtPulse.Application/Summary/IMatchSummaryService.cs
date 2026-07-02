namespace CourtPulse.Application.Summary;

/// <summary>Everything the summary engine needs — two players and their stat rows.</summary>
public sealed record SummaryInput
{
    public required long FirstPlayerKey { get; init; }
    public required string FirstPlayerName { get; init; }
    public required long SecondPlayerKey { get; init; }
    public required string SecondPlayerName { get; init; }
    public required IReadOnlyList<MatchStatLine> Stats { get; init; }
}

/// <summary>
/// Turns the api-tennis <c>statistics</c> rows of a finished match into a
/// player-facing report of strengths, weaknesses and talking points. Pure and
/// deterministic — no I/O — so it is fully unit-testable.
/// </summary>
public interface IMatchSummaryService
{
    MatchSummary Build(SummaryInput input);
}

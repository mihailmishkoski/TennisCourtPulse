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

    /// <summary>Cumulative momentum lead (First − Second); the live-list micro-indicator.</summary>
    public required double MomentumDifferential { get; init; }
}

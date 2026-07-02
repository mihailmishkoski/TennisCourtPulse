namespace CourtPulse.Application.Features.Matches.Dtos;

public sealed record MomentumPointDto
{
    public required int SetNumber { get; init; }
    public required int GameNumber { get; init; }
    public required int PointNumber { get; init; }
    public required string Beneficiary { get; init; }
    public required double Delta { get; init; }
    public string? Reason { get; init; }
    public required double FirstCumulative { get; init; }
    public required double SecondCumulative { get; init; }
    public required double FirstEwma { get; init; }
    public required double SecondEwma { get; init; }
}

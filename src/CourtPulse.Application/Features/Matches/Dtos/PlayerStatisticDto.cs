namespace CourtPulse.Application.Features.Matches.Dtos;

public sealed record StatItemDto(string Type, string Name, string Value, int? Won, int? Total);

public sealed record PlayerStatisticDto
{
    public required Guid PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required IReadOnlyList<StatItemDto> Stats { get; init; }
}

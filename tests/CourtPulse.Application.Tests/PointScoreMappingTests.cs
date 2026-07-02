using System.Text.Json;
using CourtPulse.Application.Analytics;
using CourtPulse.Application.ExternalApi;
using CourtPulse.Application.Mapping;
using Xunit;
using Xunit.Abstractions;

namespace CourtPulse.Application.Tests;

/// <summary>
/// Proves the real per-point score (e.g. "15 - 30") survives the mapper — the
/// value that fills the frontend Timeline tab. Uses the captured live payload.
/// </summary>
public sealed class PointScoreMappingTests
{
    private readonly ITestOutputHelper _output;
    private readonly LiveMatchMapper _mapper = new LiveMatchMapper();

    public PointScoreMappingTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Mapper_CarriesTheRunningGameScore_OnEveryPoint()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "tennisresults.json");
        LiveScoreEnvelope? envelope = JsonSerializer.Deserialize<LiveScoreEnvelope>(File.ReadAllText(path));
        Assert.NotNull(envelope);

        int pointsWithScore = 0;
        string? example = null;

        foreach (LiveMatchApiResponse raw in envelope!.Result)
        {
            MappedMatch mapped = _mapper.Map(raw);
            foreach (GameInput game in mapped.Games)
            {
                foreach (PointInput point in game.Points)
                {
                    if (!string.IsNullOrWhiteSpace(point.Score))
                    {
                        pointsWithScore++;
                        example ??= point.Score;
                    }
                }
            }
        }

        _output.WriteLine($"points carrying a score: {pointsWithScore}, e.g. \"{example}\"");

        Assert.True(pointsWithScore > 50, "expected many points to carry a running score");
        // The captured feed uses tokens 0/15/30/40/A separated by " - ".
        Assert.Matches(@"^(0|15|30|40|A) - (0|15|30|40|A)$", example!);
    }
}

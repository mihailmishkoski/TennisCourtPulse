using System.Text.Json;
using CourtPulse.Application.Analytics;
using CourtPulse.Application.ExternalApi;
using CourtPulse.Application.Mapping;
using CourtPulse.Application.Summary;
using Xunit;
using Xunit.Abstractions;

namespace CourtPulse.Application.Tests;

/// <summary>
/// End-to-end smoke tests over the real captured get_livescore payload: parse →
/// map → momentum + summary. Proves the whole pipeline survives the actual feed,
/// lossy points and all, and lets us eyeball the output.
/// </summary>
public sealed class RealPayloadIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly LiveMatchMapper _mapper = new LiveMatchMapper();

    public RealPayloadIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static LiveScoreEnvelope LoadEnvelope()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "tennisresults.json");
        string json = File.ReadAllText(path);
        LiveScoreEnvelope? envelope = JsonSerializer.Deserialize<LiveScoreEnvelope>(json);
        Assert.NotNull(envelope);
        return envelope!;
    }

    [Fact]
    public void EveryMatch_MapsWithoutThrowing_AndProducesMomentum()
    {
        LiveScoreEnvelope envelope = LoadEnvelope();
        MomentumCalculationService momentum = new MomentumCalculationService();

        Assert.NotEmpty(envelope.Result);

        foreach (LiveMatchApiResponse raw in envelope.Result)
        {
            MappedMatch mapped = _mapper.Map(raw);
            IReadOnlyList<MomentumSnapshotResult> snapshots = momentum.ComputeTimeline(mapped.Games);

            // Cumulative differential must equal the sum of signed deltas (sanity of the fold).
            double expected = 0.0;
            foreach (MomentumSnapshotResult s in snapshots)
            {
                expected += s.Beneficiary == Domain.Enums.PlayerSide.First ? s.Delta : -s.Delta;
            }

            double actual = snapshots.Count > 0 ? snapshots[^1].State.CumulativeDifferential : 0.0;
            Assert.Equal(expected, actual, 6);
        }
    }

    [Fact]
    public void FinishedMatchWithStats_ProducesAReadableSummary()
    {
        LiveScoreEnvelope envelope = LoadEnvelope();
        MatchSummaryService summaryService = new MatchSummaryService();

        LiveMatchApiResponse? finished = envelope.Result
            .FirstOrDefault(m => m.Statistics.Count > 0 &&
                (string.Equals(m.Status, "Finished", StringComparison.OrdinalIgnoreCase) || m.Winner is not null));
        Assert.NotNull(finished);

        MappedMatch mapped = _mapper.Map(finished!);
        MatchSummary summary = summaryService.Build(new SummaryInput
        {
            FirstPlayerKey = mapped.FirstPlayerKey,
            FirstPlayerName = mapped.FirstPlayerName,
            SecondPlayerKey = mapped.SecondPlayerKey,
            SecondPlayerName = mapped.SecondPlayerName,
            Stats = mapped.Stats
        });

        // Print it so we can read what a real summary looks like.
        _output.WriteLine($"== {mapped.FirstPlayerName} vs {mapped.SecondPlayerName} ({mapped.FinalResult}) ==");
        _output.WriteLine("First strengths: " + string.Join(" | ", summary.First.Strengths.Select(s => s.Summary)));
        _output.WriteLine("First weaknesses: " + string.Join(" | ", summary.First.Weaknesses.Select(s => s.Summary)));
        _output.WriteLine("Second strengths: " + string.Join(" | ", summary.Second.Strengths.Select(s => s.Summary)));
        _output.WriteLine("Headlines: " + string.Join(" | ", summary.Headlines));

        // A finished match with ~100 stat rows should yield at least one observation somewhere.
        int totalInsights = summary.First.Strengths.Count + summary.First.Weaknesses.Count
            + summary.Second.Strengths.Count + summary.Second.Weaknesses.Count;
        Assert.True(totalInsights > 0, "expected at least one insight from a full stat set");
    }
}

using CourtPulse.Application.Summary;
using Xunit;

namespace CourtPulse.Application.Tests;

public sealed class MatchSummaryServiceTests
{
    private readonly MatchSummaryService _service = new MatchSummaryService();

    private static MatchStatLine Stat(long player, string type, string name, string value, int? won = null, int? total = null)
    {
        return new MatchStatLine
        {
            PlayerKey = player, StatType = type, StatName = name, RawValue = value, Won = won, Total = total
        };
    }

    [Fact]
    public void StrongFirstServe_IsReportedAsAStrength_ForThatPlayerOnly()
    {
        List<MatchStatLine> stats = new List<MatchStatLine>
        {
            Stat(1, "Service", "1st serve points won", "82%", 32, 39),
            Stat(2, "Service", "1st serve points won", "48%", 19, 40)
        };

        MatchSummary summary = _service.Build(new SummaryInput
        {
            FirstPlayerKey = 1, FirstPlayerName = "Alpha",
            SecondPlayerKey = 2, SecondPlayerName = "Beta",
            Stats = stats
        });

        Assert.Contains(summary.First.Strengths, i => i.Metric.Contains("First-serve points won"));
        Assert.DoesNotContain(summary.Second.Strengths, i => i.Metric.Contains("First-serve points won"));
        Assert.Contains(summary.Second.Weaknesses, i => i.Metric.Contains("First-serve points won"));
    }

    [Fact]
    public void TinyBreakPointSample_IsDemotedToHighlight_NotAStrength()
    {
        // 1/1 break points saved = 100% but means almost nothing.
        List<MatchStatLine> stats = new List<MatchStatLine>
        {
            Stat(1, "Service", "Break Points Saved", "100%", 1, 1)
        };

        MatchSummary summary = _service.Build(new SummaryInput
        {
            FirstPlayerKey = 1, FirstPlayerName = "Alpha",
            SecondPlayerKey = 2, SecondPlayerName = "Beta",
            Stats = stats
        });

        Assert.DoesNotContain(summary.First.Strengths, i => i.Metric.Contains("Break points saved"));
        Assert.Contains(summary.First.Highlights, i => i.Metric.Contains("Break points saved"));
    }

    [Fact]
    public void HighRateFromTinySample_IsDemotedToHighlight_NotStrength()
    {
        // "100% first serve points won" off 11 points early in a match is real but
        // flimsy — it must not be trumpeted as a strength.
        List<MatchStatLine> stats = new List<MatchStatLine>
        {
            Stat(1, "Service", "1st serve points won", "100%", 11, 11),
            Stat(2, "Service", "1st serve points won", "100%", 14, 14)
        };

        MatchSummary summary = _service.Build(new SummaryInput
        {
            FirstPlayerKey = 1, FirstPlayerName = "Alpha",
            SecondPlayerKey = 2, SecondPlayerName = "Beta",
            Stats = stats
        });

        Assert.DoesNotContain(summary.First.Strengths, i => i.Metric.Contains("First-serve points won"));
        Assert.Contains(summary.First.Highlights, i => i.Metric.Contains("First-serve points won"));
    }

    [Fact]
    public void ErrorProneShotMaking_IsReportedAsWeakness()
    {
        List<MatchStatLine> stats = new List<MatchStatLine>
        {
            Stat(1, "Points", "Winners", "12"),
            Stat(1, "Points", "Unforced errors", "30")
        };

        MatchSummary summary = _service.Build(new SummaryInput
        {
            FirstPlayerKey = 1, FirstPlayerName = "Alpha",
            SecondPlayerKey = 2, SecondPlayerName = "Beta",
            Stats = stats
        });

        Assert.Contains(summary.First.Weaknesses, i => i.Metric == "Unforced errors");
    }

    [Fact]
    public void DecisiveEdge_ProducesAHeadline()
    {
        List<MatchStatLine> stats = new List<MatchStatLine>
        {
            Stat(1, "Points", "Return Points Won", "45%", 45, 100),
            Stat(2, "Points", "Return Points Won", "22%", 22, 100)
        };

        MatchSummary summary = _service.Build(new SummaryInput
        {
            FirstPlayerKey = 1, FirstPlayerName = "Alpha",
            SecondPlayerKey = 2, SecondPlayerName = "Beta",
            Stats = stats
        });

        Assert.NotEmpty(summary.Headlines);
        Assert.Contains("Alpha", summary.Headlines[0]);
    }
}

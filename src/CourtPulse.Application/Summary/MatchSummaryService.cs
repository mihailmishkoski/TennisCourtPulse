using System.Globalization;

namespace CourtPulse.Application.Summary;

/// <summary>
/// Interprets a finished match's statistics into strengths / weaknesses /
/// highlights. The design is data-driven: a table of <see cref="MetricDef"/>
/// entries maps the exact api-tennis stat names to friendly labels, and one
/// classification routine turns each metric into a verdict using both an
/// absolute bar and the head-to-head edge against the opponent.
/// </summary>
public sealed class MatchSummaryService : IMatchSummaryService
{
    private readonly SummaryThresholds _thresholds;

    // Comparable rate metrics (higher is always better for the player). Keyed by the
    // exact (stat_type, stat_name) the feed emits so we never guess.
    private static readonly IReadOnlyList<MetricDef> RateMetrics = new List<MetricDef>
    {
        new MetricDef("Service", "1st serve points won", "first-serve points won", false),
        new MetricDef("Service", "2nd serve points won", "second-serve points won", false),
        new MetricDef("Service", "1st serve percentage", "first-serve accuracy", false),
        new MetricDef("Service", "Break Points Saved", "break points saved", true),
        new MetricDef("Return", "1st return points won", "first-serve return", false),
        new MetricDef("Return", "2nd return points won", "second-serve return", false),
        new MetricDef("Return", "Break Points Converted", "break point conversion", true),
        new MetricDef("Points", "Service Points Won", "service points won", false),
        new MetricDef("Points", "Return Points Won", "return points won", false),
        new MetricDef("Points", "Net points won", "net play", true),
        new MetricDef("Games", "Return games won", "return games won", false)
    };

    public MatchSummaryService(SummaryThresholds? thresholds = null)
    {
        _thresholds = thresholds ?? SummaryThresholds.Default;
    }

    public MatchSummary Build(SummaryInput input)
    {
        Dictionary<(long, string, string), MatchStatLine> lookup =
            new Dictionary<(long, string, string), MatchStatLine>();
        foreach (MatchStatLine line in input.Stats)
        {
            lookup[(line.PlayerKey, line.StatType, line.StatName)] = line;
        }

        PlayerSummary first = BuildPlayer(input.FirstPlayerKey, input.SecondPlayerKey, lookup);
        PlayerSummary second = BuildPlayer(input.SecondPlayerKey, input.FirstPlayerKey, lookup);
        IReadOnlyList<string> headlines = BuildHeadlines(input, lookup);

        return new MatchSummary { First = first, Second = second, Headlines = headlines };
    }

    private PlayerSummary BuildPlayer(long playerKey, long opponentKey,
        Dictionary<(long, string, string), MatchStatLine> lookup)
    {
        List<StatInsight> strengths = new List<StatInsight>();
        List<StatInsight> weaknesses = new List<StatInsight>();
        List<StatInsight> highlights = new List<StatInsight>();

        foreach (MetricDef metric in RateMetrics)
        {
            MatchStatLine? mine = Get(lookup, playerKey, metric);
            if (mine is null)
            {
                continue;
            }

            double? myPct = mine.AsPercentage();
            if (myPct is null)
            {
                continue;
            }

            double? oppPct = Get(lookup, opponentKey, metric)?.AsPercentage();
            StatInsight? insight = Classify(metric, mine, myPct.Value, oppPct);
            if (insight is null)
            {
                continue;
            }

            Bucket(insight, strengths, weaknesses, highlights);
        }

        AddShotMaking(playerKey, lookup, strengths, weaknesses);
        AddServeHighlights(playerKey, lookup, strengths, weaknesses, highlights);

        return new PlayerSummary
        {
            PlayerKey = playerKey,
            Strengths = Rank(strengths),
            Weaknesses = Rank(weaknesses),
            Highlights = Rank(highlights)
        };
    }

    /// <summary>Absolute-bar + head-to-head-edge classification for one rate metric.</summary>
    private StatInsight? Classify(MetricDef metric, MatchStatLine line, double pct, double? oppPct)
    {
        double edge = oppPct.HasValue ? pct - oppPct.Value : 0.0;
        bool hasEdge = oppPct.HasValue;

        // Guard against small samples: a break point off 1 chance, or "100% first
        // serve" off 11 points early in a match, are demoted to talking points
        // rather than reported as genuine strengths/weaknesses. Pressure metrics are
        // inherently rarer, so they use a lower bar than volume metrics.
        int minSample = metric.IsPressure ? _thresholds.MinSampleForVerdict : _thresholds.MinRateSample;
        bool underSampled = line.Total.HasValue && line.Total.Value < minSample;

        bool strong = pct >= _thresholds.StrongPercentage || (hasEdge && edge >= _thresholds.DecisiveEdge);
        bool weak = pct <= _thresholds.WeakPercentage || (hasEdge && edge <= -_thresholds.DecisiveEdge);

        double weight = Math.Max(Math.Abs(edge), Math.Abs(pct - 50.0));
        string edgeText = hasEdge && Math.Abs(edge) >= _thresholds.DecisiveEdge
            ? $" ({(edge >= 0 ? "+" : "")}{edge:0} pts vs opponent)"
            : string.Empty;

        if (underSampled)
        {
            if (!strong && !weak)
            {
                return null;
            }

            return new StatInsight
            {
                Kind = InsightKind.Highlight,
                Metric = Capitalize(metric.Label),
                Summary = $"{Capitalize(metric.Label)}: {pct:0}% — but only {line.Total?.ToString() ?? "a few"} so far, too early to call.",
                PlayerValue = pct,
                OpponentValue = oppPct,
                Weight = weight
            };
        }

        if (strong && !weak)
        {
            return new StatInsight
            {
                Kind = InsightKind.Strength,
                Metric = Capitalize(metric.Label),
                Summary = $"Strong {metric.Label} — {pct:0}%{edgeText}.",
                PlayerValue = pct,
                OpponentValue = oppPct,
                Weight = weight
            };
        }

        if (weak && !strong)
        {
            return new StatInsight
            {
                Kind = InsightKind.Weakness,
                Metric = Capitalize(metric.Label),
                Summary = $"Vulnerable {metric.Label} — only {pct:0}%{edgeText}.",
                PlayerValue = pct,
                OpponentValue = oppPct,
                Weight = weight
            };
        }

        return null;
    }

    /// <summary>Winners-vs-unforced-errors: the clean shot-making / error-proneness read.</summary>
    private void AddShotMaking(long playerKey, Dictionary<(long, string, string), MatchStatLine> lookup,
        List<StatInsight> strengths, List<StatInsight> weaknesses)
    {
        double? winners = Value(lookup, playerKey, "Points", "Winners")?.AsNumber();
        double? errors = Value(lookup, playerKey, "Points", "Unforced errors")?.AsNumber();
        if (winners is null || errors is null)
        {
            return;
        }

        double ratio = winners.Value / Math.Max(1.0, errors.Value);
        double weight = Math.Abs(ratio - 1.0) * 10.0;

        if (ratio >= _thresholds.EfficientRatio)
        {
            strengths.Add(new StatInsight
            {
                Kind = InsightKind.Strength,
                Metric = "Shot-making",
                Summary = $"Clean ball-striking — {winners:0} winners to {errors:0} unforced errors.",
                PlayerValue = winners,
                OpponentValue = errors,
                Weight = weight
            });
        }
        else if (ratio <= _thresholds.LooseRatio)
        {
            weaknesses.Add(new StatInsight
            {
                Kind = InsightKind.Weakness,
                Metric = "Unforced errors",
                Summary = $"Error-prone — {errors:0} unforced errors against just {winners:0} winners.",
                PlayerValue = errors,
                OpponentValue = winners,
                Weight = weight
            });
        }
    }

    private void AddServeHighlights(long playerKey, Dictionary<(long, string, string), MatchStatLine> lookup,
        List<StatInsight> strengths, List<StatInsight> weaknesses, List<StatInsight> highlights)
    {
        double? aces = Value(lookup, playerKey, "Service", "Aces")?.AsNumber();
        if (aces.HasValue && aces.Value >= 6)
        {
            highlights.Add(new StatInsight
            {
                Kind = InsightKind.Highlight,
                Metric = "Aces",
                Summary = $"Free points on tap — {aces:0} aces.",
                PlayerValue = aces,
                Weight = aces.Value
            });
        }

        double? doubleFaults = Value(lookup, playerKey, "Service", "Double Faults")?.AsNumber();
        if (doubleFaults.HasValue && doubleFaults.Value >= 6)
        {
            weaknesses.Add(new StatInsight
            {
                Kind = InsightKind.Weakness,
                Metric = "Double faults",
                Summary = $"Second serve leaked — {doubleFaults:0} double faults.",
                PlayerValue = doubleFaults,
                Weight = doubleFaults.Value
            });
        }
    }

    private IReadOnlyList<string> BuildHeadlines(SummaryInput input,
        Dictionary<(long, string, string), MatchStatLine> lookup)
    {
        List<string> headlines = new List<string>();

        // The single most decisive comparative metric across the match.
        MetricDef? topMetric = null;
        double topEdge = 0.0;
        long topOwner = 0;
        double topOwnerPct = 0.0;

        foreach (MetricDef metric in RateMetrics)
        {
            // Pressure metrics (break points etc.) have tiny samples — a 1/1 "100%"
            // is not a headline. Judge the biggest gap on volume metrics only.
            if (metric.IsPressure)
            {
                continue;
            }

            double? firstPct = Get(lookup, input.FirstPlayerKey, metric)?.AsPercentage();
            double? secondPct = Get(lookup, input.SecondPlayerKey, metric)?.AsPercentage();
            if (firstPct is null || secondPct is null)
            {
                continue;
            }

            double edge = Math.Abs(firstPct.Value - secondPct.Value);
            if (edge > topEdge)
            {
                topEdge = edge;
                topMetric = metric;
                bool firstLeads = firstPct.Value >= secondPct.Value;
                topOwner = firstLeads ? input.FirstPlayerKey : input.SecondPlayerKey;
                topOwnerPct = firstLeads ? firstPct.Value : secondPct.Value;
            }
        }

        if (topMetric is not null && topEdge >= _thresholds.DecisiveEdge)
        {
            string owner = topOwner == input.FirstPlayerKey ? input.FirstPlayerName : input.SecondPlayerName;
            headlines.Add($"Biggest gap: {owner} owned the {topMetric.Value.Label} ({topOwnerPct:0}%, +{topEdge:0} pts).");
        }

        return headlines;
    }

    private static void Bucket(StatInsight insight, List<StatInsight> strengths,
        List<StatInsight> weaknesses, List<StatInsight> highlights)
    {
        switch (insight.Kind)
        {
            case InsightKind.Strength: strengths.Add(insight); break;
            case InsightKind.Weakness: weaknesses.Add(insight); break;
            default: highlights.Add(insight); break;
        }
    }

    private static IReadOnlyList<StatInsight> Rank(List<StatInsight> items)
    {
        items.Sort((StatInsight a, StatInsight b) => b.Weight.CompareTo(a.Weight));
        return items;
    }

    private static MatchStatLine? Get(Dictionary<(long, string, string), MatchStatLine> lookup,
        long playerKey, MetricDef metric)
    {
        return Value(lookup, playerKey, metric.StatType, metric.StatName);
    }

    private static MatchStatLine? Value(Dictionary<(long, string, string), MatchStatLine> lookup,
        long playerKey, string statType, string statName)
    {
        return lookup.TryGetValue((playerKey, statType, statName), out MatchStatLine? line) ? line : null;
    }

    private static string Capitalize(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }

        return char.ToUpper(text[0], CultureInfo.InvariantCulture) + text[1..];
    }

    private readonly record struct MetricDef(string StatType, string StatName, string Label, bool IsPressure);
}

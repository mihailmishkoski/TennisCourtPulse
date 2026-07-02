using System.Globalization;
using CourtPulse.Application.Analytics;
using CourtPulse.Application.ExternalApi;
using CourtPulse.Application.Summary;
using CourtPulse.Domain.Enums;

namespace CourtPulse.Application.Mapping;

/// <summary>
/// Anti-corruption mapper: turns a raw <see cref="LiveMatchApiResponse"/> into a
/// clean <see cref="MappedMatch"/>. All the feed's quirks are absorbed here —
/// truncated player flags, "Set 3" strings, the 0/15/30/40/A point ladder, and
/// (crucially) lossy point streams where the game-deciding point can be missing.
/// When a point's winner can't be derived, we drop that point rather than guess;
/// the reliable game-level serve_winner still carries the break signal.
/// </summary>
public sealed class LiveMatchMapper
{
    private static readonly IReadOnlyDictionary<string, int> PointRank = new Dictionary<string, int>
    {
        ["0"] = 0, ["15"] = 1, ["30"] = 2, ["40"] = 3, ["A"] = 4
    };

    public MappedMatch Map(LiveMatchApiResponse source)
    {
        bool isDoubles = (source.FirstPlayer?.Contains('/') ?? false) || (source.SecondPlayer?.Contains('/') ?? false);
        PlayerSide? winner = ParseSide(source.Winner);
        bool isFinished = string.Equals(source.Status, "Finished", StringComparison.OrdinalIgnoreCase)
            || winner.HasValue;

        (int firstPoints, int secondPoints) = ParseGameScore(source.GameResult);

        return new MappedMatch
        {
            EventKey = source.EventKey,
            TournamentKey = source.TournamentKey,
            TournamentName = source.TournamentName,
            Round = source.TournamentRound,
            EventType = source.EventType,
            FirstPlayerKey = source.FirstPlayerKey,
            FirstPlayerName = source.FirstPlayer?.Trim() ?? $"Player {source.FirstPlayerKey}",
            FirstPlayerLogo = NullIfBlank(source.FirstPlayerLogo),
            SecondPlayerKey = source.SecondPlayerKey,
            SecondPlayerName = source.SecondPlayer?.Trim() ?? $"Player {source.SecondPlayerKey}",
            SecondPlayerLogo = NullIfBlank(source.SecondPlayerLogo),
            IsDoubles = isDoubles,
            Status = source.Status,
            FinalResult = source.FinalResult,
            IsFinished = isFinished,
            Serving = ParseSide(source.Serve),
            CurrentGameFirstPoints = firstPoints,
            CurrentGameSecondPoints = secondPoints,
            Winner = winner,
            SetScores = MapScores(source.Scores),
            Games = MapGames(source.PointByPoint),
            Stats = MapStats(source.Statistics)
        };
    }

    /// <summary>
    /// Map the feed's per-set scores. api-tennis encodes a tie-break set's games
    /// with a decimal — "6.4"/"7.7" means 6 and 7 games (the ".4"/".7" is the
    /// tie-break mini-score) — so we take the integer games part rather than
    /// <c>int.Parse</c>, which would drop the whole set. The set number is taken
    /// from <c>score_set</c>, falling back to feed order only if it is missing.
    /// </summary>
    private static IReadOnlyList<SetScoreInput> MapScores(List<ApiScore> scores)
    {
        List<SetScoreInput> result = new List<SetScoreInput>();
        int fallbackSet = 0;
        foreach (ApiScore score in scores)
        {
            fallbackSet++;
            if (!TryGames(score.ScoreFirst, out int first) || !TryGames(score.ScoreSecond, out int second))
            {
                continue;
            }

            int setNumber = TryInt(score.ScoreSet, out int parsedSet) && parsedSet > 0 ? parsedSet : fallbackSet;
            result.Add(new SetScoreInput(setNumber, first, second));
        }

        return result;
    }

    /// <summary>Parse the games count from a set score, tolerating the tie-break decimal ("7.7" → 7).</summary>
    private static bool TryGames(string? value, out int games)
    {
        games = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        int dot = value.IndexOf('.');
        string gamesPart = dot >= 0 ? value[..dot] : value;
        return int.TryParse(gamesPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out games);
    }

    private IReadOnlyList<GameInput> MapGames(List<ApiGame> games)
    {
        List<GameInput> result = new List<GameInput>();
        foreach (ApiGame game in games)
        {
            int setNumber = ParseSetNumber(game.SetNumber);
            if (!TryInt(game.NumberGame, out int gameNumber))
            {
                continue;
            }

            PlayerSide? server = ParseSide(game.PlayerServed);
            if (server is null)
            {
                continue;
            }

            result.Add(new GameInput
            {
                SetNumber = setNumber,
                GameNumber = gameNumber,
                Server = server.Value,
                ServeWinner = ParseSide(game.ServeWinner),
                Points = MapPoints(game, setNumber, gameNumber, server.Value)
            });
        }

        return result;
    }

    /// <summary>
    /// Derive per-point winners from the running game-score ladder. The score is
    /// always "First - Second"; a point goes to whichever side's token advanced,
    /// or to the opponent when an advantage drops back to deuce.
    /// </summary>
    private IReadOnlyList<PointInput> MapPoints(ApiGame game, int setNumber, int gameNumber, PlayerSide server)
    {
        List<PointInput> points = new List<PointInput>();
        if (game.Points is null)
        {
            return points;
        }

        string previousFirst = "0";
        string previousSecond = "0";

        foreach (ApiPoint point in game.Points)
        {
            if (!TrySplitScore(point.Score, out string currentFirst, out string currentSecond))
            {
                continue;
            }

            PlayerSide? pointWinner = DerivePointWinner(previousFirst, previousSecond, currentFirst, currentSecond);
            previousFirst = currentFirst;
            previousSecond = currentSecond;

            if (pointWinner is null)
            {
                continue;
            }

            if (!TryInt(point.NumberPoint, out int pointNumber))
            {
                pointNumber = points.Count + 1;
            }

            points.Add(new PointInput
            {
                SetNumber = setNumber,
                GameNumber = gameNumber,
                PointNumber = pointNumber,
                Score = point.Score,
                Server = server,
                Winner = pointWinner.Value,
                IsBreakPoint = point.BreakPoint is not null,
                IsSetPoint = point.SetPoint is not null,
                IsMatchPoint = point.MatchPoint is not null
                // IsDoubleFault intentionally left false: the feed does not expose it per point.
            });
        }

        return points;
    }

    private static PlayerSide? DerivePointWinner(string prevFirst, string prevSecond, string curFirst, string curSecond)
    {
        int prevFirstRank = Rank(prevFirst);
        int prevSecondRank = Rank(prevSecond);
        int curFirstRank = Rank(curFirst);
        int curSecondRank = Rank(curSecond);

        if (curFirstRank > prevFirstRank)
        {
            return PlayerSide.First;
        }

        if (curSecondRank > prevSecondRank)
        {
            return PlayerSide.Second;
        }

        // Advantage surrendered back to deuce ⇒ the other side won the point.
        if (prevFirst == "A" && curFirst == "40")
        {
            return PlayerSide.Second;
        }

        if (prevSecond == "A" && curSecond == "40")
        {
            return PlayerSide.First;
        }

        return null;
    }

    /// <summary>
    /// Lift the match-total statistics. The feed repeats every stat once per period
    /// ("match", "set1", "set2", …); we keep only the "match" totals. Ingesting all
    /// periods would collapse them onto one row per (player, type, name) and the last
    /// one processed — a single set — would win, badly under-counting things like aces.
    /// </summary>
    private IReadOnlyList<MatchStatLine> MapStats(List<ApiStatistic> statistics)
    {
        List<MatchStatLine> result = new List<MatchStatLine>();
        foreach (ApiStatistic stat in statistics)
        {
            if (stat.StatType is null || stat.StatName is null)
            {
                continue;
            }

            if (!IsMatchTotalPeriod(stat.StatPeriod))
            {
                continue;
            }

            result.Add(new MatchStatLine
            {
                PlayerKey = stat.PlayerKey,
                StatType = stat.StatType,
                StatName = stat.StatName,
                RawValue = stat.StatValue ?? string.Empty,
                Won = stat.StatWon,
                Total = stat.StatTotal
            });
        }

        return result;
    }

    /// <summary>
    /// True for the whole-match total row. The feed uses "match"; a null/blank
    /// period is treated as a total too (defensive), while "set1"/"set2"/… are skipped.
    /// </summary>
    private static bool IsMatchTotalPeriod(string? period)
    {
        return string.IsNullOrWhiteSpace(period)
            || period.Equals("match", StringComparison.OrdinalIgnoreCase)
            || period.Equals("all", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parse the live game score (e.g. "40 - 30") into tennis-point indices
    /// (0/15/30/40/A → 0/1/2/3/4). Unknown tokens — such as tiebreak numerals —
    /// fall back to 0, since the win-probability model doesn't track in-progress
    /// tiebreak points anyway.
    /// </summary>
    private static (int First, int Second) ParseGameScore(string? gameResult)
    {
        if (!TrySplitScore(gameResult, out string first, out string second))
        {
            return (0, 0);
        }

        return (Rank(first), Rank(second));
    }

    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static PlayerSide? ParseSide(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.StartsWith("First", StringComparison.OrdinalIgnoreCase))
        {
            return PlayerSide.First;
        }

        if (value.StartsWith("Second", StringComparison.OrdinalIgnoreCase))
        {
            return PlayerSide.Second;
        }

        return null;
    }

    private static int Rank(string token)
    {
        return PointRank.TryGetValue(token, out int rank) ? rank : 0;
    }

    private static bool TrySplitScore(string? score, out string first, out string second)
    {
        first = "0";
        second = "0";
        if (string.IsNullOrWhiteSpace(score))
        {
            return false;
        }

        string[] parts = score.Split(" - ", StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        first = parts[0];
        second = parts[1];
        return true;
    }

    private static bool TryInt(string? value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Parse the first number from a set label. "Set 3" → 3, and crucially
    /// "Set 1 TieBreak" → 1 (reading trailing digits would yield 0 and invent a
    /// phantom "Set 0"). Tie-break entries thus merge into their real set.
    /// </summary>
    private static int ParseSetNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        int start = 0;
        while (start < value.Length && !char.IsDigit(value[start]))
        {
            start++;
        }

        int end = start;
        while (end < value.Length && char.IsDigit(value[end]))
        {
            end++;
        }

        return start < end && int.TryParse(value[start..end], out int result) ? result : 0;
    }
}

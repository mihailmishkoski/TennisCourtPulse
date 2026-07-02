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

    private static IReadOnlyList<(int First, int Second)> MapScores(List<ApiScore> scores)
    {
        List<(int, int)> result = new List<(int, int)>();
        foreach (ApiScore score in scores)
        {
            if (TryInt(score.ScoreFirst, out int first) && TryInt(score.ScoreSecond, out int second))
            {
                result.Add((first, second));
            }
        }

        return result;
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

    private IReadOnlyList<MatchStatLine> MapStats(List<ApiStatistic> statistics)
    {
        List<MatchStatLine> result = new List<MatchStatLine>();
        foreach (ApiStatistic stat in statistics)
        {
            if (stat.StatType is null || stat.StatName is null)
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

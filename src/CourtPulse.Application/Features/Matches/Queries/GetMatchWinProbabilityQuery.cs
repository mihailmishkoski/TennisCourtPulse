using CourtPulse.Application.Abstractions;
using CourtPulse.Application.Analytics;
using CourtPulse.Domain.Entities;
using CourtPulse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CourtPulse.Application.Features.Matches.Queries;

public sealed record GetMatchWinProbabilityQuery(Guid MatchId) : IRequest<WinProbability?>;

public sealed class GetMatchWinProbabilityQueryHandler
    : IRequestHandler<GetMatchWinProbabilityQuery, WinProbability?>
{
    private readonly ICourtPulseDbContext _db;
    private readonly WinProbabilityService _winProbability;

    public GetMatchWinProbabilityQueryHandler(ICourtPulseDbContext db, WinProbabilityService winProbability)
    {
        _db = db;
        _winProbability = winProbability;
    }

    public async Task<WinProbability?> Handle(GetMatchWinProbabilityQuery request, CancellationToken ct)
    {
        Match? match = await _db.Matches
            .Include(m => m.FirstPlayer)
            .Include(m => m.SecondPlayer)
            .Include(m => m.Sets)
            .Include(m => m.Statistics)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, ct);

        if (match?.FirstPlayer is null || match.SecondPlayer is null)
        {
            return null;
        }

        // Sets won come from the reliable FinalResult ("1 - 0"); the MatchSets rows
        // can lag or be missing for a live match, which would flatten the estimate.
        (int firstSets, int secondSets) = ParseSetsWon(match.FinalResult);

        // Current (in-progress) set's games — the last set row that isn't yet won.
        MatchSet? currentSet = match.Sets
            .OrderBy(s => s.SetNumber)
            .LastOrDefault(s => !IsSetComplete(s.ScoreFirst, s.ScoreSecond));
        bool currentSetInProgress = currentSet is not null;

        // Serve point-win rates from the stats (fallback to a neutral 0.62).
        double firstServe = ServePointWin(match, match.FirstPlayer.Id);
        double secondServe = ServePointWin(match, match.SecondPlayer.Id);

        // Current server + live game score are persisted from event_serve / event_game_result.
        PlayerSide serving = match.Serving ?? PlayerSide.First;
        bool firstServing = serving == PlayerSide.First;
        int serverPoints = firstServing ? match.CurrentFirstPoints : match.CurrentSecondPoints;
        int returnerPoints = firstServing ? match.CurrentSecondPoints : match.CurrentFirstPoints;

        return _winProbability.Estimate(new WinProbabilityInput
        {
            FirstSetsWon = firstSets,
            SecondSetsWon = secondSets,
            FirstGamesInSet = currentSetInProgress ? currentSet!.ScoreFirst : 0,
            SecondGamesInSet = currentSetInProgress ? currentSet!.ScoreSecond : 0,
            ServerPointsInGame = serverPoints,
            ReturnerPointsInGame = returnerPoints,
            Serving = serving,
            BestOfFive = false,
            FirstServePointWin = firstServe,
            SecondServePointWin = secondServe
        });
    }

    /// <summary>Parse a set score like "1 - 0" into (first, second) sets won.</summary>
    private static (int First, int Second) ParseSetsWon(string? finalResult)
    {
        if (string.IsNullOrWhiteSpace(finalResult))
        {
            return (0, 0);
        }

        string[] parts = finalResult.Split('-');
        if (parts.Length == 2
            && int.TryParse(parts[0].Trim(), out int first)
            && int.TryParse(parts[1].Trim(), out int second))
        {
            return (first, second);
        }

        return (0, 0);
    }

    /// <summary>A set is complete at 6+ with a two-game margin, or at 7 (tiebreak).</summary>
    private static bool IsSetComplete(int a, int b)
    {
        int high = Math.Max(a, b);
        int low = Math.Min(a, b);
        return (high >= 6 && high - low >= 2) || high == 7;
    }

    private static double ServePointWin(Match match, Guid playerId)
    {
        PlayerMatchStatistic? line = match.Statistics.FirstOrDefault(s =>
            s.PlayerId == playerId && s.StatType == "Points" && s.StatName == "Service Points Won");

        if (line?.StatWon is int won && line.StatTotal is int total && total > 0)
        {
            return Math.Clamp((double)won / total, 0.4, 0.85);
        }

        return 0.62;
    }
}

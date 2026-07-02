using CourtPulse.Application.Abstractions;
using CourtPulse.Application.Analytics;
using CourtPulse.Application.Mapping;
using CourtPulse.Domain.Entities;
using CourtPulse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CourtPulse.Application.Features.Sync;

public sealed record SyncLiveMatchesCommand : IRequest<SyncResult>;

public sealed record SyncResult(int MatchesProcessed, int NewPoints, int NewMomentumSnapshots, int Ended);

/// <summary>
/// The heart of the ingest: one livescore call per cycle, then a strictly
/// diff-based reconciliation — upsert tournaments/players/matches, append only
/// missing games/points, advance momentum incrementally from carried state, and
/// mark departed matches as no longer live. One bad match payload is logged and
/// skipped, never aborting the batch.
/// </summary>
public sealed class SyncLiveMatchesCommandHandler : IRequestHandler<SyncLiveMatchesCommand, SyncResult>
{
    private readonly ICourtPulseDbContext _db;
    private readonly IExternalTennisApiClient _client;
    private readonly LiveMatchMapper _mapper;
    private readonly IMomentumCalculationService _momentum;
    private readonly ILogger<SyncLiveMatchesCommandHandler> _logger;

    public SyncLiveMatchesCommandHandler(
        ICourtPulseDbContext db,
        IExternalTennisApiClient client,
        LiveMatchMapper mapper,
        IMomentumCalculationService momentum,
        ILogger<SyncLiveMatchesCommandHandler> logger)
    {
        _db = db;
        _client = client;
        _mapper = mapper;
        _momentum = momentum;
        _logger = logger;
    }

    public async Task<SyncResult> Handle(SyncLiveMatchesCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ExternalApi.LiveMatchApiResponse> raw = await _client.GetLiveMatchesAsync(cancellationToken);
        List<MappedMatch> mappedMatches = new List<MappedMatch>();
        foreach (ExternalApi.LiveMatchApiResponse response in raw)
        {
            mappedMatches.Add(_mapper.Map(response));
        }

        _logger.LogInformation("Live sync started: {Count} matches in payload", mappedMatches.Count);

        int newPoints = 0;
        int newSnapshots = 0;
        HashSet<long> seenEventKeys = new HashSet<long>();

        foreach (MappedMatch mapped in mappedMatches)
        {
            seenEventKeys.Add(mapped.EventKey);
            try
            {
                (int points, int snapshots) = await ProcessMatchAsync(mapped, cancellationToken);
                newPoints += points;
                newSnapshots += snapshots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Skipping malformed match payload {EventKey}", mapped.EventKey);
            }
        }

        int ended = await MarkDepartedMatchesAsync(seenEventKeys, cancellationToken);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry in ex.Entries)
            {
                string keys = string.Join(",", entry.Properties
                    .Where(p => p.Metadata.IsPrimaryKey())
                    .Select(p => $"{p.Metadata.Name}={p.CurrentValue}"));
                _logger.LogError("SYNC CONFLICT entity={Entity} state={State} keys={Keys}",
                    entry.Entity.GetType().Name, entry.State, keys);
            }

            throw;
        }
        _logger.LogInformation(
            "Live sync done: processed={Processed} newPoints={NewPoints} newSnapshots={NewSnapshots} ended={Ended}",
            mappedMatches.Count, newPoints, newSnapshots, ended);

        return new SyncResult(mappedMatches.Count, newPoints, newSnapshots, ended);
    }

    private async Task<(int NewPoints, int NewSnapshots)> ProcessMatchAsync(MappedMatch mapped, CancellationToken ct)
    {
        Tournament tournament = await EnsureTournamentAsync(mapped, ct);
        Player firstPlayer = await EnsurePlayerAsync(
            mapped.FirstPlayerKey, mapped.FirstPlayerName, mapped.FirstPlayerLogo, ct);
        Player secondPlayer = await EnsurePlayerAsync(
            mapped.SecondPlayerKey, mapped.SecondPlayerName, mapped.SecondPlayerLogo, ct);

        Match? match = await _db.Matches
            .Include(m => m.Games)
            .ThenInclude(g => g.Points)
            .Include(m => m.Sets)
            .Include(m => m.Statistics)
            .FirstOrDefaultAsync(m => m.ExternalEventKey == mapped.EventKey, ct);

        bool isNew = match is null;
        if (match is null)
        {
            match = new Match
            {
                Id = Guid.NewGuid(),
                ExternalEventKey = mapped.EventKey,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.Matches.Add(match);
        }

        match.TournamentId = tournament.Id;
        match.FirstPlayerId = firstPlayer.Id;
        match.SecondPlayerId = secondPlayer.Id;
        match.Round = mapped.Round;
        match.EventType = mapped.EventType;
        match.Status = mapped.Status;
        match.FinalResult = mapped.FinalResult;
        match.IsFinished = mapped.IsFinished;
        match.IsLive = !mapped.IsFinished;
        match.Serving = mapped.Serving;
        match.CurrentFirstPoints = mapped.CurrentGameFirstPoints;
        match.CurrentSecondPoints = mapped.CurrentGameSecondPoints;
        match.WinnerId = mapped.Winner switch
        {
            PlayerSide.First => firstPlayer.Id,
            PlayerSide.Second => secondPlayer.Id,
            _ => null
        };
        match.LastSyncedAt = DateTimeOffset.UtcNow;

        UpsertSets(match, mapped);
        int newPoints = UpsertGamesAndPoints(match, mapped);
        UpsertStatistics(match, mapped, firstPlayer, secondPlayer);
        int newSnapshots = AdvanceMomentum(match, mapped);

        return (newPoints, newSnapshots);
    }

    private void UpsertSets(Match match, MappedMatch mapped)
    {
        foreach (SetScoreInput setScore in mapped.SetScores)
        {
            MatchSet? existing = match.Sets.FirstOrDefault(s => s.SetNumber == setScore.SetNumber);
            if (existing is null)
            {
                match.Sets.Add(new MatchSet
                {
                    Id = Guid.NewGuid(),
                    MatchId = match.Id,
                    SetNumber = setScore.SetNumber,
                    ScoreFirst = setScore.First,
                    ScoreSecond = setScore.Second
                });
            }
            else
            {
                existing.ScoreFirst = setScore.First;
                existing.ScoreSecond = setScore.Second;
            }
        }
    }

    private int UpsertGamesAndPoints(Match match, MappedMatch mapped)
    {
        int newPoints = 0;
        foreach (GameInput gameInput in mapped.Games)
        {
            MatchGame? game = match.Games.FirstOrDefault(g =>
                g.SetNumber == gameInput.SetNumber && g.GameNumber == gameInput.GameNumber);

            if (game is null)
            {
                game = new MatchGame
                {
                    Id = Guid.NewGuid(),
                    MatchId = match.Id,
                    SetNumber = gameInput.SetNumber,
                    GameNumber = gameInput.GameNumber,
                    PlayerServed = gameInput.Server,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                match.Games.Add(game);
            }

            game.ServeWinner = gameInput.ServeWinner;

            foreach (PointInput pointInput in gameInput.Points)
            {
                bool exists = game.Points.Any(p => p.PointNumber == pointInput.PointNumber);
                if (exists)
                {
                    continue;
                }

                game.Points.Add(new MatchPoint
                {
                    Id = Guid.NewGuid(),
                    MatchGameId = game.Id,
                    PointNumber = pointInput.PointNumber,
                    Score = pointInput.Score,
                    IsBreakPoint = pointInput.IsBreakPoint,
                    IsSetPoint = pointInput.IsSetPoint,
                    IsMatchPoint = pointInput.IsMatchPoint,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                newPoints++;
            }
        }

        return newPoints;
    }

    private void UpsertStatistics(Match match, MappedMatch mapped, Player firstPlayer, Player secondPlayer)
    {
        foreach (Summary.MatchStatLine line in mapped.Stats)
        {
            Guid playerId = line.PlayerKey == mapped.FirstPlayerKey ? firstPlayer.Id
                : line.PlayerKey == mapped.SecondPlayerKey ? secondPlayer.Id
                : Guid.Empty;
            if (playerId == Guid.Empty)
            {
                continue;
            }

            PlayerMatchStatistic? existing = match.Statistics.FirstOrDefault(s =>
                s.PlayerId == playerId && s.StatType == line.StatType && s.StatName == line.StatName);

            if (existing is null)
            {
                match.Statistics.Add(new PlayerMatchStatistic
                {
                    Id = Guid.NewGuid(),
                    MatchId = match.Id,
                    PlayerId = playerId,
                    StatType = line.StatType,
                    StatName = line.StatName,
                    StatValue = line.RawValue,
                    StatWon = line.Won,
                    StatTotal = line.Total,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                existing.StatValue = line.RawValue;
                existing.StatWon = line.Won;
                existing.StatTotal = line.Total;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    /// <summary>
    /// Fold only the games that have completed since we last looked, carrying the
    /// stored momentum state forward. Games are marked processed so a break is
    /// never counted twice across cycles.
    /// </summary>
    private int AdvanceMomentum(Match match, MappedMatch mapped)
    {
        List<GameInput> newlyCompleted = new List<GameInput>();
        foreach (GameInput gameInput in mapped.Games.OrderBy(g => g.SetNumber).ThenBy(g => g.GameNumber))
        {
            if (!gameInput.IsComplete)
            {
                continue;
            }

            MatchGame? entity = match.Games.FirstOrDefault(g =>
                g.SetNumber == gameInput.SetNumber && g.GameNumber == gameInput.GameNumber);
            if (entity is null || entity.MomentumProcessed)
            {
                continue;
            }

            newlyCompleted.Add(gameInput);
            entity.MomentumProcessed = true;
        }

        if (newlyCompleted.Count == 0)
        {
            return 0;
        }

        MomentumState priorState = new MomentumState
        {
            FirstCumulative = match.MomentumFirstCumulative,
            SecondCumulative = match.MomentumSecondCumulative,
            FirstEwma = match.MomentumFirstEwma,
            SecondEwma = match.MomentumSecondEwma
        };

        MomentumProgression progression = _momentum.Advance(priorState, newlyCompleted);

        foreach (MomentumSnapshotResult snapshot in progression.Snapshots)
        {
            match.MomentumSnapshots.Add(new MomentumSnapshot
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                SetNumber = snapshot.SetNumber,
                GameNumber = snapshot.GameNumber,
                PointNumber = snapshot.PointNumber,
                Beneficiary = snapshot.Beneficiary,
                Delta = snapshot.Delta,
                Reason = snapshot.Reason,
                FirstCumulative = snapshot.State.FirstCumulative,
                SecondCumulative = snapshot.State.SecondCumulative,
                FirstEwma = snapshot.State.FirstEwma,
                SecondEwma = snapshot.State.SecondEwma,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        match.MomentumFirstCumulative = progression.State.FirstCumulative;
        match.MomentumSecondCumulative = progression.State.SecondCumulative;
        match.MomentumFirstEwma = progression.State.FirstEwma;
        match.MomentumSecondEwma = progression.State.SecondEwma;

        return progression.Snapshots.Count;
    }

    private async Task<Tournament> EnsureTournamentAsync(MappedMatch mapped, CancellationToken ct)
    {
        // Check entities already added this cycle (Local) before the DB — otherwise
        // two matches sharing a tournament each insert it and SaveChanges duplicates.
        Tournament? tournament = _db.Tournaments.Local.FirstOrDefault(t => t.ExternalKey == mapped.TournamentKey)
            ?? await _db.Tournaments.FirstOrDefaultAsync(t => t.ExternalKey == mapped.TournamentKey, ct);
        if (tournament is not null)
        {
            return tournament;
        }

        tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            ExternalKey = mapped.TournamentKey,
            Name = mapped.TournamentName ?? $"Tournament {mapped.TournamentKey}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Tournaments.Add(tournament);
        return tournament;
    }

    private async Task<Player> EnsurePlayerAsync(long externalKey, string name, string? logoUrl, CancellationToken ct)
    {
        int key = (int)externalKey;
        Player? player = _db.Players.Local.FirstOrDefault(p => p.ExternalKey == key)
            ?? await _db.Players.FirstOrDefaultAsync(p => p.ExternalKey == key, ct);
        if (player is not null)
        {
            // Backfill a logo the first time the feed provides one.
            if (player.LogoUrl is null && logoUrl is not null)
            {
                player.LogoUrl = logoUrl;
            }

            return player;
        }

        player = new Player
        {
            Id = Guid.NewGuid(),
            ExternalKey = key,
            FullName = name,
            LogoUrl = logoUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Players.Add(player);
        return player;
    }

    private async Task<int> MarkDepartedMatchesAsync(HashSet<long> seenEventKeys, CancellationToken ct)
    {
        List<Match> stillLive = await _db.Matches
            .Where(m => m.IsLive)
            .ToListAsync(ct);

        int ended = 0;
        foreach (Match match in stillLive)
        {
            if (seenEventKeys.Contains(match.ExternalEventKey))
            {
                continue;
            }

            match.IsLive = false;
            match.IsFinished = true;
            ended++;
        }

        return ended;
    }
}

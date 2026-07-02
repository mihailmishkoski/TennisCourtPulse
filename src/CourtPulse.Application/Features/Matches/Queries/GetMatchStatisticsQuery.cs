using CourtPulse.Application.Abstractions;
using CourtPulse.Application.Features.Matches.Dtos;
using CourtPulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CourtPulse.Application.Features.Matches.Queries;

public sealed record GetMatchStatisticsQuery(Guid MatchId) : IRequest<IReadOnlyList<PlayerStatisticDto>?>;

public sealed class GetMatchStatisticsQueryHandler
    : IRequestHandler<GetMatchStatisticsQuery, IReadOnlyList<PlayerStatisticDto>?>
{
    private readonly ICourtPulseDbContext _db;

    public GetMatchStatisticsQueryHandler(ICourtPulseDbContext db) => _db = db;

    public async Task<IReadOnlyList<PlayerStatisticDto>?> Handle(GetMatchStatisticsQuery request, CancellationToken ct)
    {
        Match? match = await _db.Matches
            .Include(m => m.FirstPlayer)
            .Include(m => m.SecondPlayer)
            .Include(m => m.Statistics)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, ct);

        if (match is null)
        {
            return null;
        }

        List<PlayerStatisticDto> result = new List<PlayerStatisticDto>();
        foreach (Player? player in new[] { match.FirstPlayer, match.SecondPlayer })
        {
            if (player is null)
            {
                continue;
            }

            result.Add(new PlayerStatisticDto
            {
                PlayerId = player.Id,
                PlayerName = player.FullName,
                Stats = match.Statistics
                    .Where(s => s.PlayerId == player.Id)
                    .Select(s => new StatItemDto(s.StatType, s.StatName, s.StatValue, s.StatWon, s.StatTotal))
                    .ToList()
            });
        }

        return result;
    }
}

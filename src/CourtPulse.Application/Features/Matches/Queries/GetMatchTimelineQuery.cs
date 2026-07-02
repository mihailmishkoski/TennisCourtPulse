using CourtPulse.Application.Abstractions;
using CourtPulse.Application.Features.Matches.Dtos;
using CourtPulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CourtPulse.Application.Features.Matches.Queries;

public sealed record GetMatchTimelineQuery(Guid MatchId) : IRequest<MatchTimelineDto?>;

public sealed class GetMatchTimelineQueryHandler : IRequestHandler<GetMatchTimelineQuery, MatchTimelineDto?>
{
    private readonly ICourtPulseDbContext _db;

    public GetMatchTimelineQueryHandler(ICourtPulseDbContext db) => _db = db;

    public async Task<MatchTimelineDto?> Handle(GetMatchTimelineQuery request, CancellationToken ct)
    {
        Match? match = await _db.Matches
            .Include(m => m.Games)
            .ThenInclude(g => g.Points)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, ct);

        if (match is null)
        {
            return null;
        }

        List<TimelineSetDto> sets = match.Games
            .GroupBy(g => g.SetNumber)
            .OrderBy(group => group.Key)
            .Select(group => new TimelineSetDto
            {
                SetNumber = group.Key,
                Games = group.OrderBy(g => g.GameNumber).Select(g => new TimelineGameDto
                {
                    GameNumber = g.GameNumber,
                    Server = g.PlayerServed.ToString(),
                    ServeWinner = g.ServeWinner?.ToString(),
                    Points = g.Points
                        .OrderBy(p => p.PointNumber)
                        .Select(p => new TimelinePointDto(
                            p.PointNumber, p.Score, p.IsBreakPoint, p.IsSetPoint, p.IsMatchPoint))
                        .ToList()
                }).ToList()
            }).ToList();

        return new MatchTimelineDto { MatchId = match.Id, Sets = sets };
    }
}

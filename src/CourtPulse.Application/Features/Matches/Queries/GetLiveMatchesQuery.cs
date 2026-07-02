using CourtPulse.Application.Abstractions;
using CourtPulse.Application.Features.Matches.Dtos;
using CourtPulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CourtPulse.Application.Features.Matches.Queries;

public sealed record GetLiveMatchesQuery : IRequest<IReadOnlyList<LiveMatchSummaryDto>>;

public sealed class GetLiveMatchesQueryHandler
    : IRequestHandler<GetLiveMatchesQuery, IReadOnlyList<LiveMatchSummaryDto>>
{
    private readonly ICourtPulseDbContext _db;

    public GetLiveMatchesQueryHandler(ICourtPulseDbContext db) => _db = db;

    public async Task<IReadOnlyList<LiveMatchSummaryDto>> Handle(GetLiveMatchesQuery request, CancellationToken ct)
    {
        List<Match> matches = await _db.Matches
            .Include(m => m.Tournament)
            .Include(m => m.FirstPlayer)
            .Include(m => m.SecondPlayer)
            .Where(m => m.IsLive || m.IsFinished)
            .OrderByDescending(m => m.IsLive)
            .ThenByDescending(m => m.LastSyncedAt)
            .Take(200)
            .ToListAsync(ct);

        return matches.Select(m => new LiveMatchSummaryDto
        {
            Id = m.Id,
            Tournament = m.Tournament?.Name ?? string.Empty,
            FirstPlayer = m.FirstPlayer?.FullName ?? string.Empty,
            FirstPlayerLogo = m.FirstPlayer?.LogoUrl,
            SecondPlayer = m.SecondPlayer?.FullName ?? string.Empty,
            SecondPlayerLogo = m.SecondPlayer?.LogoUrl,
            Status = m.Status,
            IsLive = m.IsLive,
            IsFinished = m.IsFinished,
            FinalResult = m.FinalResult,
            MomentumDifferential = m.MomentumFirstCumulative - m.MomentumSecondCumulative
        }).ToList();
    }
}

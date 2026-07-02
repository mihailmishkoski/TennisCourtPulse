using CourtPulse.Application.Abstractions;
using CourtPulse.Application.Features.Matches.Dtos;
using CourtPulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CourtPulse.Application.Features.Matches.Queries;

public sealed record GetMatchByIdQuery(Guid MatchId) : IRequest<MatchDetailDto?>;

public sealed class GetMatchByIdQueryHandler : IRequestHandler<GetMatchByIdQuery, MatchDetailDto?>
{
    private readonly ICourtPulseDbContext _db;

    public GetMatchByIdQueryHandler(ICourtPulseDbContext db) => _db = db;

    public async Task<MatchDetailDto?> Handle(GetMatchByIdQuery request, CancellationToken ct)
    {
        Match? match = await _db.Matches
            .Include(m => m.Tournament)
            .Include(m => m.FirstPlayer)
            .Include(m => m.SecondPlayer)
            .Include(m => m.Winner)
            .Include(m => m.Sets)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, ct);

        if (match is null)
        {
            return null;
        }

        return new MatchDetailDto
        {
            Id = match.Id,
            Tournament = match.Tournament?.Name ?? string.Empty,
            Round = match.Round,
            EventType = match.EventType,
            FirstPlayer = match.FirstPlayer?.FullName ?? string.Empty,
            FirstPlayerLogo = match.FirstPlayer?.LogoUrl,
            SecondPlayer = match.SecondPlayer?.FullName ?? string.Empty,
            SecondPlayerLogo = match.SecondPlayer?.LogoUrl,
            Status = match.Status,
            IsLive = match.IsLive,
            IsFinished = match.IsFinished,
            FinalResult = match.FinalResult,
            Winner = match.Winner?.FullName,
            Sets = match.Sets
                .OrderBy(s => s.SetNumber)
                .Select(s => new SetScoreDto(s.SetNumber, s.ScoreFirst, s.ScoreSecond))
                .ToList()
        };
    }
}

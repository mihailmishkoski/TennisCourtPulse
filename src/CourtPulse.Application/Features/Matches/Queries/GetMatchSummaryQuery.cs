using CourtPulse.Application.Abstractions;
using CourtPulse.Application.Summary;
using CourtPulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CourtPulse.Application.Features.Matches.Queries;

public sealed record GetMatchSummaryQuery(Guid MatchId) : IRequest<MatchSummary?>;

public sealed class GetMatchSummaryQueryHandler : IRequestHandler<GetMatchSummaryQuery, MatchSummary?>
{
    private readonly ICourtPulseDbContext _db;
    private readonly IMatchSummaryService _summary;

    public GetMatchSummaryQueryHandler(ICourtPulseDbContext db, IMatchSummaryService summary)
    {
        _db = db;
        _summary = summary;
    }

    public async Task<MatchSummary?> Handle(GetMatchSummaryQuery request, CancellationToken ct)
    {
        Match? match = await _db.Matches
            .Include(m => m.FirstPlayer)
            .Include(m => m.SecondPlayer)
            .Include(m => m.Statistics)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, ct);

        if (match?.FirstPlayer is null || match.SecondPlayer is null)
        {
            return null;
        }

        long firstKey = match.FirstPlayer.ExternalKey;
        long secondKey = match.SecondPlayer.ExternalKey;

        List<MatchStatLine> lines = match.Statistics.Select(s => new MatchStatLine
        {
            PlayerKey = s.PlayerId == match.FirstPlayer.Id ? firstKey : secondKey,
            StatType = s.StatType,
            StatName = s.StatName,
            RawValue = s.StatValue,
            Won = s.StatWon,
            Total = s.StatTotal
        }).ToList();

        return _summary.Build(new SummaryInput
        {
            FirstPlayerKey = firstKey,
            FirstPlayerName = match.FirstPlayer.FullName,
            SecondPlayerKey = secondKey,
            SecondPlayerName = match.SecondPlayer.FullName,
            Stats = lines
        });
    }
}

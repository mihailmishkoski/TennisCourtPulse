using CourtPulse.Application.Abstractions;
using CourtPulse.Application.Features.Matches.Dtos;
using CourtPulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CourtPulse.Application.Features.Matches.Queries;

public sealed record GetMatchMomentumQuery(Guid MatchId) : IRequest<IReadOnlyList<MomentumPointDto>?>;

public sealed class GetMatchMomentumQueryHandler
    : IRequestHandler<GetMatchMomentumQuery, IReadOnlyList<MomentumPointDto>?>
{
    private readonly ICourtPulseDbContext _db;

    public GetMatchMomentumQueryHandler(ICourtPulseDbContext db) => _db = db;

    public async Task<IReadOnlyList<MomentumPointDto>?> Handle(GetMatchMomentumQuery request, CancellationToken ct)
    {
        bool exists = await _db.Matches.AnyAsync(m => m.Id == request.MatchId, ct);
        if (!exists)
        {
            return null;
        }

        List<MomentumSnapshot> snapshots = await _db.MomentumSnapshots
            .Where(s => s.MatchId == request.MatchId)
            .OrderBy(s => s.SetNumber).ThenBy(s => s.GameNumber).ThenBy(s => s.PointNumber)
            .ToListAsync(ct);

        return snapshots.Select(s => new MomentumPointDto
        {
            SetNumber = s.SetNumber,
            GameNumber = s.GameNumber,
            PointNumber = s.PointNumber,
            Beneficiary = s.Beneficiary.ToString(),
            Delta = s.Delta,
            Reason = s.Reason,
            FirstCumulative = s.FirstCumulative,
            SecondCumulative = s.SecondCumulative,
            FirstEwma = s.FirstEwma,
            SecondEwma = s.SecondEwma
        }).ToList();
    }
}

using CourtPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourtPulse.Application.Abstractions;

/// <summary>
/// The persistence surface the Application layer is allowed to see. Keeping it an
/// interface (rather than referencing the concrete DbContext) preserves the
/// dependency direction — Application depends on abstractions, Infrastructure
/// implements them.
/// </summary>
public interface ICourtPulseDbContext
{
    DbSet<Tournament> Tournaments { get; }
    DbSet<Player> Players { get; }
    DbSet<Match> Matches { get; }
    DbSet<MatchSet> MatchSets { get; }
    DbSet<MatchGame> MatchGames { get; }
    DbSet<MatchPoint> MatchPoints { get; }
    DbSet<PlayerMatchStatistic> PlayerMatchStatistics { get; }
    DbSet<MomentumSnapshot> MomentumSnapshots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

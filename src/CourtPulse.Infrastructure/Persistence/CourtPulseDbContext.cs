using CourtPulse.Application.Abstractions;
using CourtPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourtPulse.Infrastructure.Persistence;

public sealed class CourtPulseDbContext : DbContext, ICourtPulseDbContext
{
    public CourtPulseDbContext(DbContextOptions<CourtPulseDbContext> options) : base(options)
    {
    }

    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchSet> MatchSets => Set<MatchSet>();
    public DbSet<MatchGame> MatchGames => Set<MatchGame>();
    public DbSet<MatchPoint> MatchPoints => Set<MatchPoint>();
    public DbSet<PlayerMatchStatistic> PlayerMatchStatistics => Set<PlayerMatchStatistic>();
    public DbSet<MomentumSnapshot> MomentumSnapshots => Set<MomentumSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CourtPulseDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

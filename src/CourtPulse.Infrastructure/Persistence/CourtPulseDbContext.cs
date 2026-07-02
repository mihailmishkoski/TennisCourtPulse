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

        // All primary keys are Guids assigned in code (Guid.NewGuid()). Left as the
        // EF default (ValueGeneratedOnAdd), a NEW child added to an already-tracked
        // parent is mistaken for an existing row and issued as an UPDATE that affects
        // 0 rows — which fails the whole sync the moment a live match gains a new
        // set/game/point. Marking them ValueGeneratedNever makes EF treat them as
        // genuine inserts.
        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableProperty property in modelBuilder.Model
                     .GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.IsPrimaryKey() && p.ClrType == typeof(Guid)))
        {
            property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
        }

        base.OnModelCreating(modelBuilder);
    }
}

using CourtPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtPulse.Infrastructure.Persistence.Configurations;

public sealed class MomentumSnapshotConfiguration : IEntityTypeConfiguration<MomentumSnapshot>
{
    public void Configure(EntityTypeBuilder<MomentumSnapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.MatchId, s.SetNumber, s.GameNumber, s.PointNumber });
        builder.Property(s => s.Reason).HasMaxLength(120);
        builder.HasOne<Match>().WithMany(m => m.MomentumSnapshots)
            .HasForeignKey(s => s.MatchId).OnDelete(DeleteBehavior.Cascade);
    }
}

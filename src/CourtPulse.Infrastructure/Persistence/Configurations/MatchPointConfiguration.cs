using CourtPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtPulse.Infrastructure.Persistence.Configurations;

public sealed class MatchPointConfiguration : IEntityTypeConfiguration<MatchPoint>
{
    public void Configure(EntityTypeBuilder<MatchPoint> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.MatchGameId, p.PointNumber }).IsUnique();
        builder.Property(p => p.Score).HasMaxLength(20);
        builder.HasOne<MatchGame>().WithMany(g => g.Points)
            .HasForeignKey(p => p.MatchGameId).OnDelete(DeleteBehavior.Cascade);
    }
}

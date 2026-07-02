using CourtPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtPulse.Infrastructure.Persistence.Configurations;

public sealed class MatchGameConfiguration : IEntityTypeConfiguration<MatchGame>
{
    public void Configure(EntityTypeBuilder<MatchGame> builder)
    {
        builder.HasKey(g => g.Id);
        builder.HasIndex(g => new { g.MatchId, g.SetNumber, g.GameNumber }).IsUnique();
        builder.Property(g => g.Score).HasMaxLength(20);
        builder.HasOne<Match>().WithMany(m => m.Games)
            .HasForeignKey(g => g.MatchId).OnDelete(DeleteBehavior.Cascade);
    }
}

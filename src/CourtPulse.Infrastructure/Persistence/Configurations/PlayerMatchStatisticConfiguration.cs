using CourtPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtPulse.Infrastructure.Persistence.Configurations;

public sealed class PlayerMatchStatisticConfiguration : IEntityTypeConfiguration<PlayerMatchStatistic>
{
    public void Configure(EntityTypeBuilder<PlayerMatchStatistic> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.MatchId, s.PlayerId, s.StatType, s.StatName }).IsUnique();
        builder.Property(s => s.StatPeriod).HasMaxLength(30);
        builder.Property(s => s.StatType).HasMaxLength(60);
        builder.Property(s => s.StatName).HasMaxLength(120);
        builder.Property(s => s.StatValue).HasMaxLength(60);
        builder.HasOne<Match>().WithMany(m => m.Statistics)
            .HasForeignKey(s => s.MatchId).OnDelete(DeleteBehavior.Cascade);
    }
}

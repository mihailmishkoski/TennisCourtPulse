using CourtPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtPulse.Infrastructure.Persistence.Configurations;

public sealed class MatchSetConfiguration : IEntityTypeConfiguration<MatchSet>
{
    public void Configure(EntityTypeBuilder<MatchSet> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.MatchId, s.SetNumber }).IsUnique();
        builder.HasOne<Match>().WithMany(m => m.Sets)
            .HasForeignKey(s => s.MatchId).OnDelete(DeleteBehavior.Cascade);
    }
}

using CourtPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtPulse.Infrastructure.Persistence.Configurations;

public sealed class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
{
    public void Configure(EntityTypeBuilder<Tournament> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.ExternalKey).IsUnique();
        builder.Property(t => t.Name).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Season).HasMaxLength(50);
    }
}

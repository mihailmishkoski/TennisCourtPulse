using CourtPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtPulse.Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => m.ExternalEventKey).IsUnique();
        builder.HasIndex(m => m.IsLive);
        builder.HasIndex(m => m.EventDate);
        builder.Property(m => m.Status).HasMaxLength(50);
        builder.Property(m => m.EventType).HasMaxLength(120);
        builder.Property(m => m.Round).HasMaxLength(120);
        builder.Property(m => m.FinalResult).HasMaxLength(50);

        builder.HasOne(m => m.Tournament).WithMany(t => t.Matches)
            .HasForeignKey(m => m.TournamentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.FirstPlayer).WithMany()
            .HasForeignKey(m => m.FirstPlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.SecondPlayer).WithMany()
            .HasForeignKey(m => m.SecondPlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Winner).WithMany()
            .HasForeignKey(m => m.WinnerId).OnDelete(DeleteBehavior.Restrict);
    }
}

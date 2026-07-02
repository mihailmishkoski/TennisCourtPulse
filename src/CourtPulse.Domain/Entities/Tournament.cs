namespace CourtPulse.Domain.Entities;

/// <summary>A tournament, keyed to the external feed by <see cref="ExternalKey"/>.</summary>
public sealed class Tournament
{
    public Guid Id { get; set; }
    public int ExternalKey { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Season { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Match> Matches { get; set; } = new List<Match>();
}

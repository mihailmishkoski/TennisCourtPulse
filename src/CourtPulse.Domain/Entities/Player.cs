namespace CourtPulse.Domain.Entities;

/// <summary>A player (or, for doubles, a pair) keyed by external id.</summary>
public sealed class Player
{
    public Guid Id { get; set; }
    public int ExternalKey { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

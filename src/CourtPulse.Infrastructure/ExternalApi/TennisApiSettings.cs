namespace CourtPulse.Infrastructure.ExternalApi;

/// <summary>Bound from configuration (never hard-coded). See appsettings + user-secrets.</summary>
public sealed class TennisApiSettings
{
    public const string SectionName = "TennisApi";

    public string BaseUrl { get; set; } = "https://api.api-tennis.com/tennis/";
    public string ApiKey { get; set; } = string.Empty;
    public int SyncIntervalSeconds { get; set; } = 25;

    /// <summary>
    /// Dev-only: when set to a captured get_livescore JSON file, the sync ingests
    /// that file instead of calling the live API — handy when no matches are live.
    /// </summary>
    public string? SampleDataPath { get; set; }
}

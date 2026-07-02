using System.Text.Json.Serialization;

namespace CourtPulse.Application.ExternalApi;

public sealed class ApiScore
{
    [JsonPropertyName("score_first")]
    public string? ScoreFirst { get; set; }

    [JsonPropertyName("score_second")]
    public string? ScoreSecond { get; set; }

    [JsonPropertyName("score_set")]
    public string? ScoreSet { get; set; }
}

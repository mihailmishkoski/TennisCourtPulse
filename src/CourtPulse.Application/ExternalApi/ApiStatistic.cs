using System.Text.Json.Serialization;

namespace CourtPulse.Application.ExternalApi;

public sealed class ApiStatistic
{
    [JsonPropertyName("player_key")]
    public long PlayerKey { get; set; }

    [JsonPropertyName("stat_period")]
    public string? StatPeriod { get; set; }

    [JsonPropertyName("stat_type")]
    public string? StatType { get; set; }

    [JsonPropertyName("stat_name")]
    public string? StatName { get; set; }

    [JsonPropertyName("stat_value")]
    public string? StatValue { get; set; }

    [JsonPropertyName("stat_won")]
    public int? StatWon { get; set; }

    [JsonPropertyName("stat_total")]
    public int? StatTotal { get; set; }
}

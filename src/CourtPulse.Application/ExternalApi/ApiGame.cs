using System.Text.Json.Serialization;

namespace CourtPulse.Application.ExternalApi;

public sealed class ApiGame
{
    [JsonPropertyName("set_number")]
    public string? SetNumber { get; set; }

    [JsonPropertyName("number_game")]
    public string? NumberGame { get; set; }

    [JsonPropertyName("player_served")]
    public string? PlayerServed { get; set; }

    [JsonPropertyName("serve_winner")]
    public string? ServeWinner { get; set; }

    [JsonPropertyName("serve_lost")]
    public string? ServeLost { get; set; }

    [JsonPropertyName("score")]
    public string? Score { get; set; }

    [JsonPropertyName("points")]
    public List<ApiPoint>? Points { get; set; }
}

public sealed class ApiPoint
{
    [JsonPropertyName("number_point")]
    public string? NumberPoint { get; set; }

    [JsonPropertyName("score")]
    public string? Score { get; set; }

    [JsonPropertyName("break_point")]
    public string? BreakPoint { get; set; }

    [JsonPropertyName("set_point")]
    public string? SetPoint { get; set; }

    [JsonPropertyName("match_point")]
    public string? MatchPoint { get; set; }
}

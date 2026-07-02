using System.Text.Json.Serialization;

namespace CourtPulse.Application.ExternalApi;

/// <summary>
/// One match as delivered by api-tennis. Mirrors the raw wire shape 1:1
/// (snake_case, everything-as-string) and is an anti-corruption boundary —
/// nothing outside the mapper should touch it.
/// </summary>
public sealed class LiveMatchApiResponse
{
    [JsonPropertyName("event_key")]
    public long EventKey { get; set; }

    [JsonPropertyName("event_date")]
    public string? EventDate { get; set; }

    [JsonPropertyName("event_time")]
    public string? EventTime { get; set; }

    [JsonPropertyName("event_first_player")]
    public string? FirstPlayer { get; set; }

    [JsonPropertyName("first_player_key")]
    public long FirstPlayerKey { get; set; }

    [JsonPropertyName("event_second_player")]
    public string? SecondPlayer { get; set; }

    [JsonPropertyName("second_player_key")]
    public long SecondPlayerKey { get; set; }

    [JsonPropertyName("event_final_result")]
    public string? FinalResult { get; set; }

    [JsonPropertyName("event_game_result")]
    public string? GameResult { get; set; }

    [JsonPropertyName("event_serve")]
    public string? Serve { get; set; }

    [JsonPropertyName("event_winner")]
    public string? Winner { get; set; }

    [JsonPropertyName("event_status")]
    public string? Status { get; set; }

    [JsonPropertyName("event_type_type")]
    public string? EventType { get; set; }

    [JsonPropertyName("tournament_name")]
    public string? TournamentName { get; set; }

    [JsonPropertyName("tournament_key")]
    public int TournamentKey { get; set; }

    [JsonPropertyName("tournament_round")]
    public string? TournamentRound { get; set; }

    [JsonPropertyName("tournament_season")]
    public string? TournamentSeason { get; set; }

    [JsonPropertyName("event_live")]
    public string? Live { get; set; }

    [JsonPropertyName("event_first_player_logo")]
    public string? FirstPlayerLogo { get; set; }

    [JsonPropertyName("event_second_player_logo")]
    public string? SecondPlayerLogo { get; set; }

    [JsonPropertyName("scores")]
    public List<ApiScore> Scores { get; set; } = new List<ApiScore>();

    [JsonPropertyName("pointbypoint")]
    public List<ApiGame> PointByPoint { get; set; } = new List<ApiGame>();

    [JsonPropertyName("statistics")]
    public List<ApiStatistic> Statistics { get; set; } = new List<ApiStatistic>();
}

using System.Text.Json.Serialization;

namespace CourtPulse.Application.ExternalApi;

/// <summary>Root of the api-tennis <c>get_livescore</c> response.</summary>
public sealed class LiveScoreEnvelope
{
    [JsonPropertyName("success")]
    public int Success { get; set; }

    [JsonPropertyName("result")]
    public List<LiveMatchApiResponse> Result { get; set; } = new List<LiveMatchApiResponse>();
}

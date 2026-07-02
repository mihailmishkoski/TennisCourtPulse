using System.Text.Json;
using CourtPulse.Application.Abstractions;
using CourtPulse.Application.ExternalApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CourtPulse.Infrastructure.ExternalApi;

/// <summary>
/// Typed HttpClient over api-tennis. The API is query-string driven and wants the
/// key in the URL (not a header). Resilience (retry/backoff) is layered on at DI
/// registration via Polly; this class just shapes the request and parses it.
/// </summary>
public sealed class TennisApiClient : IExternalTennisApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly TennisApiSettings _settings;
    private readonly ILogger<TennisApiClient> _logger;

    public TennisApiClient(HttpClient httpClient, IOptions<TennisApiSettings> settings, ILogger<TennisApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LiveMatchApiResponse>> GetLiveMatchesAsync(CancellationToken cancellationToken)
    {
        string requestUri = $"?method=get_livescore&APIkey={Uri.EscapeDataString(_settings.ApiKey)}";

        using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        LiveScoreEnvelope? envelope =
            await JsonSerializer.DeserializeAsync<LiveScoreEnvelope>(stream, JsonOptions, cancellationToken);

        if (envelope is null || envelope.Success != 1)
        {
            _logger.LogWarning("Livescore call returned no usable payload (success flag not set)");
            return Array.Empty<LiveMatchApiResponse>();
        }

        return envelope.Result;
    }
}

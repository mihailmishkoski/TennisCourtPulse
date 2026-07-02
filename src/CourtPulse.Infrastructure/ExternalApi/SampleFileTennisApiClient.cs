using System.Text.Json;
using CourtPulse.Application.Abstractions;
using CourtPulse.Application.ExternalApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CourtPulse.Infrastructure.ExternalApi;

/// <summary>
/// Dev-only client that returns matches from a captured JSON file instead of the
/// live API. Registered in place of <see cref="TennisApiClient"/> when
/// <see cref="TennisApiSettings.SampleDataPath"/> is configured — lets the full
/// pipeline (mapping → momentum → summary) run when nothing is live.
/// </summary>
public sealed class SampleFileTennisApiClient : IExternalTennisApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly TennisApiSettings _settings;
    private readonly ILogger<SampleFileTennisApiClient> _logger;

    public SampleFileTennisApiClient(IOptions<TennisApiSettings> settings, ILogger<SampleFileTennisApiClient> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LiveMatchApiResponse>> GetLiveMatchesAsync(CancellationToken cancellationToken)
    {
        string path = _settings.SampleDataPath!;
        if (!File.Exists(path))
        {
            _logger.LogWarning("Sample data file not found at {Path}", path);
            return Array.Empty<LiveMatchApiResponse>();
        }

        await using FileStream stream = File.OpenRead(path);
        LiveScoreEnvelope? envelope =
            await JsonSerializer.DeserializeAsync<LiveScoreEnvelope>(stream, JsonOptions, cancellationToken);

        _logger.LogInformation("Loaded {Count} matches from sample file", envelope?.Result.Count ?? 0);
        return envelope?.Result ?? new List<LiveMatchApiResponse>();
    }
}

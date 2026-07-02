using CourtPulse.Application.ExternalApi;

namespace CourtPulse.Application.Abstractions;

/// <summary>
/// Wraps the api-tennis HTTP feed. One call returns every live match in a single
/// payload — implementations must never call per-match.
/// </summary>
public interface IExternalTennisApiClient
{
    Task<IReadOnlyList<LiveMatchApiResponse>> GetLiveMatchesAsync(CancellationToken cancellationToken);
}

using CourtPulse.Application.Features.Sync;
using CourtPulse.Infrastructure.ExternalApi;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CourtPulse.Infrastructure.BackgroundServices;

/// <summary>
/// In-process poller. On a fixed cadence it opens a fresh DI scope (because this
/// service is a singleton while the DbContext/mediator must be scoped) and fires
/// the diff-based <see cref="SyncLiveMatchesCommand"/>. A failed cycle is logged
/// and the loop continues — one bad poll never kills the service.
/// </summary>
public sealed class LiveMatchSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TennisApiSettings _settings;
    private readonly ILogger<LiveMatchSyncBackgroundService> _logger;

    public LiveMatchSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<TennisApiSettings> settings,
        ILogger<LiveMatchSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromSeconds(Math.Max(5, _settings.SyncIntervalSeconds));
        using PeriodicTimer timer = new PeriodicTimer(interval);

        _logger.LogInformation("Live match sync running every {Seconds}s", interval.TotalSeconds);

        do
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new SyncLiveMatchesCommand(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Live sync cycle failed; will retry next tick");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

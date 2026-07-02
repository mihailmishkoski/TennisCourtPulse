using CourtPulse.Application.Analytics;
using CourtPulse.Application.Mapping;
using CourtPulse.Application.Summary;
using Microsoft.Extensions.DependencyInjection;

namespace CourtPulse.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the pure analytics engines, the mapper, and MediatR handlers.
    /// The engines are stateless and thread-safe, so they live as singletons.
    /// </summary>
    public static IServiceCollection AddCourtPulseApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddSingleton<IMomentumCalculationService, MomentumCalculationService>();
        services.AddSingleton<IMatchSummaryService, MatchSummaryService>();
        services.AddSingleton<WinProbabilityService>();
        services.AddSingleton<TurningPointDetector>();
        services.AddSingleton<LiveMatchMapper>();

        return services;
    }
}

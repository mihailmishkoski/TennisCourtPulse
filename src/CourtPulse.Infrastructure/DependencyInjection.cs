using CourtPulse.Application.Abstractions;
using CourtPulse.Infrastructure.BackgroundServices;
using CourtPulse.Infrastructure.ExternalApi;
using CourtPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace CourtPulse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCourtPulseInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TennisApiSettings>(configuration.GetSection(TennisApiSettings.SectionName));

        string connectionString = configuration.GetConnectionString("CourtPulse")
            ?? throw new InvalidOperationException("Missing connection string 'CourtPulse'.");

        services.AddDbContext<CourtPulseDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICourtPulseDbContext>(provider => provider.GetRequiredService<CourtPulseDbContext>());

        string? sampleDataPath = configuration[$"{TennisApiSettings.SectionName}:SampleDataPath"];
        if (!string.IsNullOrWhiteSpace(sampleDataPath))
        {
            // Dev sample mode: ingest a captured file instead of calling the live API.
            services.AddSingleton<IExternalTennisApiClient, SampleFileTennisApiClient>();
        }
        else
        {
            services.AddHttpClient<IExternalTennisApiClient, TennisApiClient>((provider, client) =>
                {
                    TennisApiSettings settings = provider
                        .GetRequiredService<Microsoft.Extensions.Options.IOptions<TennisApiSettings>>().Value;
                    client.BaseAddress = new Uri(settings.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                // Trial plans are rate-limited: back off politely rather than hammering.
                .AddTransientHttpErrorPolicy(policy =>
                    policy.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
        }

        services.AddHostedService<LiveMatchSyncBackgroundService>();

        return services;
    }
}

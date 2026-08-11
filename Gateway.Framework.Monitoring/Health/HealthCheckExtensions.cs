using Gateway.Framework.Monitoring.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.Framework.Monitoring.Health;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddGatewayHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecks = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Gateway is running."), tags: [HealthTags.Live, HealthTags.Gateway]);

        var downstreamUrls = configuration.GetSection("HealthChecks:DownstreamUrls").Get<string[]>() ?? Array.Empty<string>();
        foreach (var url in downstreamUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
        {
            healthChecks.AddUrlGroup(new Uri(url), name: $"downstream-{url}", tags: [HealthTags.Ready, HealthTags.Downstream]);
        }

        return services;
    }

    public static WebApplication MapGatewayHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthTags.Live)
        });

        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthTags.Ready) || check.Tags.Contains(HealthTags.Gateway)
        });

        return app;
    }
}

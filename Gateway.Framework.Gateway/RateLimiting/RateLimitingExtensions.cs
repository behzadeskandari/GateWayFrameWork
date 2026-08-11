using System.Threading.RateLimiting;
using Gateway.Framework.Gateway.RateLimiting;
using Gateway.Framework.Logging.Audit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Framework.Gateway.RateLimiting;

public static class RateLimitingExtensions
{
    public const string PolicyName = "GatewayFixedWindow";

    public static IServiceCollection AddGatewayRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));
        var options = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();

        if (!options.Enabled)
        {
            return services;
        }

        services.AddRateLimiter(limiter =>
        {
            limiter.AddFixedWindowLimiter(PolicyName, config =>
            {
                config.PermitLimit = options.PermitLimit;
                config.Window = TimeSpan.FromSeconds(options.WindowSeconds);
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = options.QueueLimit;
            });

            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var auditLogger = context.HttpContext.RequestServices.GetService<IAuditLogger>();
                if (auditLogger is not null)
                {
                    await auditLogger.LogAsync(
                        AuditActions.RateLimitRejected,
                        context.HttpContext.Request.Path,
                        AuditOutcomes.Denied,
                        AuditContext.FromHttpContext(context.HttpContext),
                        cancellationToken);
                }
            };
        });

        return services;
    }
}

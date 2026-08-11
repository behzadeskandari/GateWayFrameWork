using Gateway.Framework.Core.Observability.Correlation;
using Gateway.Framework.Gateway.Middleware;
using Gateway.Framework.Gateway.RateLimiting;
using Gateway.Framework.Gateway.Versioning;
using Gateway.Framework.Gateway.Yarp;
using Gateway.Framework.Monitoring.Health;
using Gateway.Framework.Security.Authentication;
using Gateway.Framework.Security.Authorization;
using Gateway.Framework.Security.TokenValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gateway.Framework.Gateway;

public static class GatewayServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayFramework(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CorrelationIdOptions>(configuration.GetSection(CorrelationIdOptions.SectionName));
        services.AddGatewayInputValidation(configuration);
        services.AddGatewayRateLimiting(configuration);
        services.AddGatewayApiVersioning();
        services.AddGatewayReverseProxy(configuration);
        return services;
    }
}

public static class GatewayApplicationBuilderExtensions
{
    public static WebApplication UseGatewayMiddleware(this WebApplication app)
    {
        app.UseGatewayGlobalExceptionHandling();
        app.UseGatewayCorrelationId();
        app.UseGatewayRequestResponseLogging();
        app.UseGatewayInputValidation();

        var rateLimitOptions = app.Configuration
            .GetSection(RateLimitOptions.SectionName)
            .Get<RateLimitOptions>() ?? new RateLimitOptions();

        if (rateLimitOptions.Enabled)
        {
            app.UseRateLimiter();
        }

        return app;
    }

    public static WebApplication MapGatewayEndpoints(this WebApplication app)
    {
        var authOptions = app.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        var authRequired = TokenValidationExtensions.IsAuthenticationRequired(app.Environment, authOptions);
        var rateLimitEnabled = (app.Configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions()).Enabled;

        app.MapGatewayHealthChecks();

        var controllers = app.MapControllers();
        var reverseProxy = app.MapReverseProxy();

        if (rateLimitEnabled)
        {
            controllers.RequireRateLimiting(RateLimitingExtensions.PolicyName);
            reverseProxy.RequireRateLimiting(RateLimitingExtensions.PolicyName);
        }

        if (authRequired)
        {
            var policy = authOptions.ValidateScopes && authOptions.RequiredScopes.Length > 0
                ? GatewayPolicies.RequiredScopes
                : GatewayPolicies.AuthenticatedUser;
            reverseProxy.RequireAuthorization(policy);
        }

        return app;
    }
}

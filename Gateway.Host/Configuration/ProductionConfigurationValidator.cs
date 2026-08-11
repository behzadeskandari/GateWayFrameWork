using Gateway.Framework.Infrastructure.Configuration;
using Gateway.Framework.Plugins.Configuration;
using Gateway.Framework.Security.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gateway.Host.Configuration;

public static class ProductionConfigurationValidator
{
    public static void Validate(WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            return;
        }

        var auth = app.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        if (!auth.Enabled)
        {
            throw new InvalidOperationException("Production requires Auth:Enabled=true.");
        }

        if (string.IsNullOrWhiteSpace(auth.Authority))
        {
            throw new InvalidOperationException("Production requires Auth:Authority.");
        }

        if (string.IsNullOrWhiteSpace(auth.Audience))
        {
            throw new InvalidOperationException("Production requires Auth:Audience.");
        }

        if (auth.AllowDevelopmentAnonymous)
        {
            throw new InvalidOperationException("Auth:AllowDevelopmentAnonymous is forbidden in Production.");
        }

        var gateway = app.Configuration.GetSection(GatewayOptions.SectionName).Get<GatewayOptions>() ?? new GatewayOptions();
        if (gateway.EnableDetailedErrors)
        {
            throw new InvalidOperationException("Gateway:EnableDetailedErrors must be false in Production.");
        }

        ValidateNoLocalhostDownstream(app.Configuration);
        ValidateEnabledPlugins(app.Configuration);
    }

    private static void ValidateNoLocalhostDownstream(IConfiguration configuration)
    {
        var clusters = configuration.GetSection("ReverseProxy:Clusters").GetChildren();
        foreach (var cluster in clusters)
        {
            var destinations = cluster.GetSection("Destinations").GetChildren();
            foreach (var destination in destinations)
            {
                var address = destination["Address"];
                if (!string.IsNullOrWhiteSpace(address) &&
                    address.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Production cannot use localhost downstream address: {address}");
                }
            }
        }
    }

    private static void ValidateEnabledPlugins(IConfiguration configuration)
    {
        foreach (var pluginSection in configuration.GetSection("Plugins").GetChildren())
        {
            var options = pluginSection.Get<PluginOptions>() ?? new PluginOptions();
            PluginOptionsValidator.ValidateProduction(pluginSection.Key, options);
        }
    }
}

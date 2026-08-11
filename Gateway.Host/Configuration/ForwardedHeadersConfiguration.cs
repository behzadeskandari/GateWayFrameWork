using System.Net;
using Microsoft.Extensions.Configuration;

namespace Gateway.Host.Configuration;

public sealed class ForwardedHeadersOptionsSetup
{
    public const string SectionName = "Security:ForwardedHeaders";

    public bool Enabled { get; set; }
    public string[] KnownProxies { get; set; } = Array.Empty<string>();
}

public static class ForwardedHeadersConfiguration
{
    public static ForwardedHeadersOptions Configure(IConfiguration configuration)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
        };

        var settings = configuration.GetSection(ForwardedHeadersOptionsSetup.SectionName)
            .Get<ForwardedHeadersOptionsSetup>() ?? new ForwardedHeadersOptionsSetup();

        if (!settings.Enabled)
        {
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            return options;
        }

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var proxy in settings.KnownProxies.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (IPAddress.TryParse(proxy, out var ip))
            {
                options.KnownProxies.Add(ip);
            }
        }

        return options;
    }
}

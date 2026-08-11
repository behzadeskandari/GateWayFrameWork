using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Framework.Plugins.Abstractions;

public sealed class BankingGatewayPluginContext
{
    public BankingGatewayPluginContext(
        IServiceCollection services,
        IConfiguration configuration,
        BankingGatewayPluginMetadata metadata)
    {
        Services = services;
        Configuration = configuration;
        Metadata = metadata;
    }

    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }
    public BankingGatewayPluginMetadata Metadata { get; }

    public List<(string Name, Type CheckType)> HealthChecks { get; } = [];
}
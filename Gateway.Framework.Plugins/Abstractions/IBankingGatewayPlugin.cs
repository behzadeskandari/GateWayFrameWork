using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Gateway.Framework.Plugins.Context;

namespace Gateway.Framework.Plugins.Abstractions;

public interface IBankingGatewayPlugin
{
    BankingGatewayPluginMetadata Metadata { get; }

    bool IsEnabled(IConfiguration configuration);

    void ConfigureServices(BankingGatewayPluginContext context);

    void ConfigureRoutes(PluginRouteBuilder routes, IConfiguration pluginConfiguration);

    Task InitializeAsync(BankingGatewayPluginContext context, CancellationToken cancellationToken = default);

    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

public interface IBankingGatewayPluginManager
{
    IReadOnlyCollection<BankingGatewayPluginStatus> GetPluginStatuses();

    IReadOnlyCollection<BankingGatewayPluginMetadata> GetLoadedPlugins();

    IReadOnlyCollection<BankingPluginCapability> GetAvailableCapabilities();

    IReadOnlyCollection<PluginRouteDefinition> GetPluginRoutes();

    IReadOnlyCollection<PluginClusterDefinition> GetPluginClusters();

    Task InitializeAsync(CancellationToken cancellationToken = default);
}

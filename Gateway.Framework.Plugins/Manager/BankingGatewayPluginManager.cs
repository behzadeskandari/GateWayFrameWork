using Gateway.Framework.Core.Abstractions;
using Gateway.Framework.Core.Observability.Correlation;
using Gateway.Framework.Logging.Audit;
using Gateway.Framework.Plugins.Abstractions;
using Gateway.Framework.Plugins.Manager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gateway.Framework.Plugins.Manager;

public sealed class BankingGatewayPluginManager : IBankingGatewayPluginManager
{
    public const string FrameworkVersion = "1.0.0";

    private readonly IEnumerable<IBankingGatewayPlugin> _plugins;
    private readonly PluginRouteRegistry _registry;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BankingGatewayPluginManager> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BankingGatewayPluginManager(
        IEnumerable<IBankingGatewayPlugin> plugins,
        PluginRouteRegistry registry,
        IConfiguration configuration,
        ILogger<BankingGatewayPluginManager> logger,
        IServiceProvider serviceProvider)
    {
        _plugins = plugins;
        _registry = registry;
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public IReadOnlyCollection<BankingGatewayPluginStatus> GetPluginStatuses() =>
        _registry.Statuses;

    public IReadOnlyCollection<BankingGatewayPluginMetadata> GetLoadedPlugins() =>
        _registry.LoadedPlugins;

    public IReadOnlyCollection<BankingPluginCapability> GetAvailableCapabilities()
    {
        var capabilities = new HashSet<BankingPluginCapability>();
        foreach (var plugin in _registry.LoadedPlugins)
        {
            foreach (BankingPluginCapability value in Enum.GetValues<BankingPluginCapability>())
            {
                if (value != BankingPluginCapability.None && plugin.Capabilities.HasFlag(value))
                {
                    capabilities.Add(value);
                }
            }
        }

        return capabilities.ToList();
    }

    public IReadOnlyCollection<PluginRouteDefinition> GetPluginRoutes() => _registry.Routes;

    public IReadOnlyCollection<PluginClusterDefinition> GetPluginClusters() => _registry.Clusters;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        foreach (var plugin in _plugins)
        {
            var metadata = plugin.Metadata;
            var status = _registry.Statuses.FirstOrDefault(s => s.BankCode == metadata.BankCode);
            if (status is null || !status.Enabled)
            {
                continue;
            }

            try
            {
                var section = _configuration.GetSection($"Plugins:{metadata.ConfigurationKey}");
                var context = new BankingGatewayPluginContext(new ServiceCollection(), section, metadata);
                await plugin.InitializeAsync(context, cancellationToken);
                _registry.UpdateStatus(metadata.BankCode, current => current with
                {
                    State = BankingGatewayPluginState.Initialized
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin {BankCode} initialization failed.", metadata.BankCode);
                var auditLogger = _serviceProvider.GetService<IAuditLogger>();
                if (auditLogger is not null)
                {
                    await auditLogger.LogAsync(
                        AuditActions.PluginInitializationFailure,
                        $"/plugins/{metadata.BankCode}",
                        AuditOutcomes.Failure,
                        new Dictionary<string, string>
                        {
                            ["bankCode"] = metadata.BankCode,
                            ["reason"] = ex.GetType().Name
                        },
                        cancellationToken);
                }

                _registry.UpdateStatus(metadata.BankCode, current => current with
                {
                    State = BankingGatewayPluginState.Failed,
                    Error = ex.Message
                });
            }
        }
    }
}

internal sealed class BankingGatewayPluginInitializationHostedService : IHostedService
{
    private readonly IBankingGatewayPluginManager _manager;

    public BankingGatewayPluginInitializationHostedService(IBankingGatewayPluginManager manager) =>
        _manager = manager;

    public Task StartAsync(CancellationToken cancellationToken) =>
        _manager.InitializeAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

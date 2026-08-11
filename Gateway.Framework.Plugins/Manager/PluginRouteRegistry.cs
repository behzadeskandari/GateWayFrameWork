using Gateway.Framework.Plugins.Abstractions;

namespace Gateway.Framework.Plugins.Manager;

public sealed class PluginRouteRegistry
{
    private readonly List<PluginRouteDefinition> _routes = [];
    private readonly List<PluginClusterDefinition> _clusters = [];
    private readonly List<BankingGatewayPluginMetadata> _loaded = [];
    private readonly Dictionary<string, BankingGatewayPluginStatus> _statuses = new(StringComparer.OrdinalIgnoreCase);

    public void AddRoutes(IEnumerable<PluginRouteDefinition> routes) => _routes.AddRange(routes);

    public void AddClusters(IEnumerable<PluginClusterDefinition> clusters) => _clusters.AddRange(clusters);

    public void AddLoadedPlugin(BankingGatewayPluginMetadata metadata) => _loaded.Add(metadata);

    public void SetStatus(BankingGatewayPluginStatus status) => _statuses[status.BankCode] = status;

    public IReadOnlyCollection<PluginRouteDefinition> Routes => _routes.AsReadOnly();

    public IReadOnlyCollection<PluginClusterDefinition> Clusters => _clusters.AsReadOnly();

    public IReadOnlyCollection<BankingGatewayPluginMetadata> LoadedPlugins => _loaded.AsReadOnly();

    public IReadOnlyCollection<BankingGatewayPluginStatus> Statuses => _statuses.Values.ToList();

    public void UpdateStatus(string bankCode, Func<BankingGatewayPluginStatus, BankingGatewayPluginStatus> update)
    {
        if (_statuses.TryGetValue(bankCode, out var current))
        {
            _statuses[bankCode] = update(current);
        }
    }
}

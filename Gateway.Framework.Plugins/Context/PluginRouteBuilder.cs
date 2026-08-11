using Gateway.Framework.Plugins.Abstractions;

namespace Gateway.Framework.Plugins.Context;

public sealed class PluginRouteBuilder
{
    private readonly string _bankCode;
    private readonly List<PluginRouteDefinition> _routes = [];
    private readonly List<PluginClusterDefinition> _clusters = [];

    public PluginRouteBuilder(string bankCode) => _bankCode = bankCode;

    public PluginRouteBuilder AddRoute(
        string routeSuffix,
        string path,
        string destinationAddress,
        string? pathRemovePrefix = null,
        string? pathPrefix = null,
        bool requiresFinancialResilience = false)
    {
        var clusterId = $"{_bankCode.ToLowerInvariant()}-{routeSuffix}-cluster";
        var routeId = $"{_bankCode.ToLowerInvariant()}-{routeSuffix}-route";

        _routes.Add(new PluginRouteDefinition
        {
            RouteId = routeId,
            ClusterId = clusterId,
            Path = path,
            PathRemovePrefix = pathRemovePrefix,
            PathPrefix = pathPrefix,
            RequiresFinancialResilience = requiresFinancialResilience
        });

        _clusters.Add(new PluginClusterDefinition
        {
            ClusterId = clusterId,
            DestinationAddress = destinationAddress,
            HttpClientName = requiresFinancialResilience ? Resilience.ResilienceExtensions.FinancialHttpClientName : null
        });

        return this;
    }

    public IReadOnlyCollection<PluginRouteDefinition> Routes => _routes;
    public IReadOnlyCollection<PluginClusterDefinition> Clusters => _clusters;
}

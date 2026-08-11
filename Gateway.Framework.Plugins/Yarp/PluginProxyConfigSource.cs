using Gateway.Framework.Plugins.Manager;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Framework.Plugins.Yarp;

public interface IPluginProxyConfigSource
{
    (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) GetPluginProxyConfig();
}

public sealed class PluginProxyConfigSource : IPluginProxyConfigSource
{
    private readonly PluginRouteRegistry _registry;

    public PluginProxyConfigSource(PluginRouteRegistry registry) => _registry = registry;

    public (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) GetPluginProxyConfig()
    {
        var routes = _registry.Routes.Select(route =>
        {
            var transforms = new List<IReadOnlyDictionary<string, string>>();
            if (!string.IsNullOrWhiteSpace(route.PathRemovePrefix))
            {
                transforms.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PathRemovePrefix"] = route.PathRemovePrefix
                });
            }

            if (!string.IsNullOrWhiteSpace(route.PathPrefix))
            {
                transforms.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PathPrefix"] = route.PathPrefix
                });
            }

            return new RouteConfig
            {
                RouteId = route.RouteId,
                ClusterId = route.ClusterId,
                Match = new RouteMatch { Path = route.Path },
                Transforms = transforms
            };
        }).ToList();

        var clusters = _registry.Clusters.Select(cluster => new ClusterConfig
        {
            ClusterId = cluster.ClusterId,
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["primary"] = new DestinationConfig { Address = cluster.DestinationAddress }
            },
            Metadata = string.IsNullOrWhiteSpace(cluster.HttpClientName)
                ? null
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["FinancialHttpClient"] = cluster.HttpClientName
                }
        }).ToList();

        return (routes, clusters);
    }
}

public sealed class PluginProxyConfigProvider : IProxyConfigProvider
{
    private readonly IPluginProxyConfigSource _source;
    private volatile PluginProxyConfig _config;

    public PluginProxyConfigProvider(IPluginProxyConfigSource source)
    {
        _source = source;
        _config = BuildConfig();
    }

    public IProxyConfig GetConfig() => _config;

    private PluginProxyConfig BuildConfig()
    {
        var (routes, clusters) = _source.GetPluginProxyConfig();
        return new PluginProxyConfig(routes, clusters);
    }

    private sealed class PluginProxyConfig : IProxyConfig
    {
        public PluginProxyConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(CancellationToken.None);
        }

        public IReadOnlyList<RouteConfig> Routes { get; }
        public IReadOnlyList<ClusterConfig> Clusters { get; }
        public IChangeToken ChangeToken { get; }
    }
}

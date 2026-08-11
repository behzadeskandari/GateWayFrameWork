namespace Gateway.Framework.Plugins.Abstractions;

public sealed class PluginRouteDefinition
{
    public required string RouteId { get; init; }
    public required string ClusterId { get; init; }
    public required string Path { get; init; }
    public string? PathRemovePrefix { get; init; }
    public string? PathPrefix { get; init; }
    public bool RequiresFinancialResilience { get; init; }
}

public sealed class PluginClusterDefinition
{
    public required string ClusterId { get; init; }
    public required string DestinationAddress { get; init; }
    public string? HttpClientName { get; init; }
}

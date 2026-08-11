namespace Gateway.Framework.Infrastructure.Configuration;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public string ServiceName { get; set; } = "Gateway.Host";
    public string EnvironmentName { get; set; } = "Development";
    public bool EnableDetailedErrors { get; set; }
}

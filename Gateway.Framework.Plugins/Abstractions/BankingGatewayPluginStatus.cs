namespace Gateway.Framework.Plugins.Abstractions;

public enum BankingGatewayPluginState
{
    Disabled,
    Registered,
    Initialized,
    Failed
}

public sealed record BankingGatewayPluginStatus(
    string BankCode,
    string Name,
    string Version,
    BankingGatewayPluginState State,
    bool Enabled,
    BankingPluginCapability Capabilities,
    string? Error = null);

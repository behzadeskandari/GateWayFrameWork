namespace Gateway.Framework.Plugins.Abstractions;

public sealed record BankingGatewayPluginMetadata(
    string Name,
    string BankCode,
    string Version,
    string FrameworkVersion,
    string ConfigurationKey,
    BankingPluginCapability Capabilities);

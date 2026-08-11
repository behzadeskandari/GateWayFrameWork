using Gateway.Framework.Plugins.Abstractions;
using Gateway.Framework.Plugins.Configuration;
using Gateway.Framework.Plugins.Context;
using Gateway.Framework.Monitoring.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Gateway.Framework.Plugins.Extensions;
using Gateway.Framework.Plugins.Manager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Bank.Bank1;

public sealed class Bank1Plugin : IBankingGatewayPlugin
{
    public BankingGatewayPluginMetadata Metadata { get; } = new(
        Name: "Sample Bank One",
        BankCode: "BANK1",
        Version: "1.0.0",
        FrameworkVersion: BankingGatewayPluginManager.FrameworkVersion,
        ConfigurationKey: "Bank1",
        Capabilities: BankingPluginCapability.Accounts | BankingPluginCapability.Balance);

    public bool IsEnabled(IConfiguration configuration) =>
        configuration.GetSection("Plugins:Bank1").Get<Bank1Options>()?.Enabled ?? false;

    public void ConfigureServices(BankingGatewayPluginContext context)
    {
        context.Services.Configure<Bank1Options>(context.Configuration);
        context.AddBankPluginHttpClient<Bank1AccountsClient>("bank1-accounts");
        context.Services.AddSingleton<IBank1AccountsService, Bank1AccountsService>();
        context.Services.AddHealthChecks()
            .AddTypeActivatedCheck<Health.Bank1HealthCheck>(
                "plugin-bank1",
                failureStatus: null,
                tags: [HealthTags.Plugin, HealthTags.Ready],
                context.Configuration);
    }

    public void ConfigureRoutes(PluginRouteBuilder routes, IConfiguration pluginConfiguration)
    {
        var options = pluginConfiguration.Get<Bank1Options>() ?? new Bank1Options();
        routes.AddRoute(
            routeSuffix: "bank1",
            path: "/api/v1/banks/bank1/{**catch-all}",
            destinationAddress: options.BaseUrl,
            pathRemovePrefix: "/api/v1/banks/bank1",
            pathPrefix: "/api");
    }

    public Task InitializeAsync(BankingGatewayPluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ShutdownAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

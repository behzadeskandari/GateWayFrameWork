using Gateway.Framework.Plugins.Abstractions;
using Gateway.Framework.Plugins.Context;
using Gateway.Framework.Monitoring.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Gateway.Framework.Plugins.Extensions;
using Gateway.Framework.Plugins.Manager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Bank.Bank2;

public sealed class Bank2Plugin : IBankingGatewayPlugin
{
    public BankingGatewayPluginMetadata Metadata { get; } = new(
        Name: "Sample Bank Two",
        BankCode: "BANK2",
        Version: "1.0.0",
        FrameworkVersion: BankingGatewayPluginManager.FrameworkVersion,
        ConfigurationKey: "Bank2",
        Capabilities: BankingPluginCapability.Payment | BankingPluginCapability.Transfer);

    public bool IsEnabled(IConfiguration configuration) =>
        configuration.GetSection("Plugins:Bank2").Get<Bank2Options>()?.Enabled ?? false;

    public void ConfigureServices(BankingGatewayPluginContext context)
    {
        context.Services.Configure<Bank2Options>(context.Configuration);
        context.AddBankPluginHttpClient<Bank2PaymentsClient>("bank2-payments", financialOperations: true);
        context.Services.AddSingleton<IBank2PaymentsService, Bank2PaymentsService>();
        context.Services.AddHealthChecks()
            .AddTypeActivatedCheck<Health.Bank2HealthCheck>(
                "plugin-bank2",
                failureStatus: null,
                tags: [HealthTags.Plugin, HealthTags.Ready],
                context.Configuration);
    }

    public void ConfigureRoutes(PluginRouteBuilder routes, IConfiguration pluginConfiguration)
    {
        var options = pluginConfiguration.Get<Bank2Options>() ?? new Bank2Options();
        routes.AddRoute(
            routeSuffix: "bank2",
            path: "/api/v1/banks/bank2/{**catch-all}",
            destinationAddress: options.BaseUrl,
            pathRemovePrefix: "/api/v1/banks/bank2",
            pathPrefix: "/api",
            requiresFinancialResilience: true);
    }

    public Task InitializeAsync(BankingGatewayPluginContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ShutdownAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

using Gateway.Framework.Plugins.Abstractions;
using Gateway.Framework.Plugins.Configuration;
using Gateway.Framework.Plugins.Context;
using Gateway.Framework.Plugins.Extensions;
using Gateway.Framework.Plugins.Manager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gateway.Tests.Unit;

public sealed class PluginManagerTests
{
    [Fact]
    public void AddPlugin_WithDuplicateBankCode_Throws()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Plugins:Bank1:Enabled"] = "false",
                ["Plugins:Bank2:Enabled"] = "false"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddBankingGatewayPlugins(configuration, plugins =>
            {
                plugins.AddPlugin<DuplicateBankCodePluginA>();
                plugins.AddPlugin<DuplicateBankCodePluginB>();
            }));

        Assert.Contains("Duplicate BankCode", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPlugin_WhenEnabledWithoutBaseUrl_Throws()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Plugins:Bank1:Enabled"] = "true"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddBankingGatewayPlugins(configuration, plugins => plugins.AddPlugin<SampleTestPlugin>()));
        Assert.Contains("BaseUrl", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPlugin_RegistersRoutesForEnabledPlugin()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Plugins:Bank1:Enabled"] = "true",
                ["Plugins:Bank1:BaseUrl"] = "http://localhost:5201/",
                ["Plugins:Bank1:TimeoutSeconds"] = "30"
            })
            .Build();

        services.AddBankingGatewayPlugins(configuration, plugins => plugins.AddPlugin<SampleTestPlugin>());
        var registry = services.Single(descriptor => descriptor.ServiceType == typeof(PluginRouteRegistry)).ImplementationInstance as PluginRouteRegistry;

        Assert.NotNull(registry);
        Assert.Single(registry!.Routes);
        Assert.Equal("/api/v1/banks/test/accounts/{**catch-all}", registry.Routes.First().Path);
    }

    private class SampleTestPlugin : IBankingGatewayPlugin
    {
        public BankingGatewayPluginMetadata Metadata { get; } = new(
            "Test Plugin",
            "BANK1",
            "1.0.0",
            BankingGatewayPluginManager.FrameworkVersion,
            "Bank1",
            BankingPluginCapability.Accounts);

        public bool IsEnabled(IConfiguration configuration) =>
            configuration.GetSection("Plugins:Bank1").Get<PluginOptions>()?.Enabled ?? false;

        public void ConfigureServices(BankingGatewayPluginContext context) { }

        public void ConfigureRoutes(PluginRouteBuilder routes, IConfiguration pluginConfiguration) =>
            routes.AddRoute("accounts", "/api/v1/banks/test/accounts/{**catch-all}", pluginConfiguration["BaseUrl"] ?? "http://localhost/");

        public Task InitializeAsync(BankingGatewayPluginContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ShutdownAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class DuplicateBankCodePluginA : SampleTestPlugin;

    private sealed class DuplicateBankCodePluginB : SampleTestPlugin;
}

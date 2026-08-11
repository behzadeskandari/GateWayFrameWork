using Gateway.Framework.Plugins.Abstractions;
using Gateway.Framework.Plugins.Configuration;
using Gateway.Framework.Plugins.Context;
using Gateway.Framework.Plugins.Manager;
using Gateway.Framework.Plugins.Yarp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gateway.Framework.Plugins.Extensions;

public static class PluginServiceCollectionExtensions
{
    public static IServiceCollection AddBankingGatewayPlugins(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<BankingGatewayPluginBuilder> configurePlugins)
    {
        var registry = new PluginRouteRegistry();
        services.TryAddSingleton(registry);

        var builder = new BankingGatewayPluginBuilder(services, configuration, registry);
        configurePlugins(builder);

        services.TryAddSingleton<IBankingGatewayPluginManager, BankingGatewayPluginManager>();
        services.AddHostedService<BankingGatewayPluginInitializationHostedService>();
        services.AddSingleton<IPluginProxyConfigSource, PluginProxyConfigSource>();

        return services;
    }
}

public sealed class BankingGatewayPluginBuilder
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;
    private readonly PluginRouteRegistry _registry;
    private readonly HashSet<string> _bankCodes = new(StringComparer.OrdinalIgnoreCase);

    internal BankingGatewayPluginBuilder(
        IServiceCollection services,
        IConfiguration configuration,
        PluginRouteRegistry registry)
    {
        _services = services;
        _configuration = configuration;
        _registry = registry;
    }

    public BankingGatewayPluginBuilder AddPlugin<TPlugin>()
        where TPlugin : class, IBankingGatewayPlugin, new()
    {
        var plugin = new TPlugin();
        RegisterPlugin(typeof(TPlugin), plugin);
        return this;
    }

    private void RegisterPlugin(Type pluginType, IBankingGatewayPlugin plugin)
    {
        var metadata = plugin.Metadata;
        ValidateMetadata(metadata);

        if (!_bankCodes.Add(metadata.BankCode))
        {
            throw new InvalidOperationException($"Duplicate BankCode detected: {metadata.BankCode}");
        }

        _services.AddSingleton(typeof(IBankingGatewayPlugin), pluginType);
        _services.AddSingleton(pluginType);

        var enabled = plugin.IsEnabled(_configuration);
        var section = _configuration.GetSection($"Plugins:{metadata.ConfigurationKey}");
        var options = section.Get<PluginOptions>() ?? new PluginOptions();

        if (enabled)
        {
            PluginOptionsValidator.Validate(metadata.ConfigurationKey, options);

            var context = new BankingGatewayPluginContext(_services, section, metadata);
            plugin.ConfigureServices(context);

            var routeBuilder = new PluginRouteBuilder(metadata.BankCode);
            plugin.ConfigureRoutes(routeBuilder, section);
            _registry.AddRoutes(routeBuilder.Routes);
            _registry.AddClusters(routeBuilder.Clusters);
            _registry.AddLoadedPlugin(metadata);
        }

        _registry.SetStatus(new BankingGatewayPluginStatus(
            metadata.BankCode,
            metadata.Name,
            metadata.Version,
            enabled ? BankingGatewayPluginState.Registered : BankingGatewayPluginState.Disabled,
            enabled,
            metadata.Capabilities));
    }

    private static void ValidateMetadata(BankingGatewayPluginMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata.BankCode))
        {
            throw new InvalidOperationException("Plugin BankCode is required.");
        }

        if (string.IsNullOrWhiteSpace(metadata.Name))
        {
            throw new InvalidOperationException("Plugin Name is required.");
        }

        if (string.IsNullOrWhiteSpace(metadata.Version))
        {
            throw new InvalidOperationException("Plugin Version is required.");
        }

        if (!string.Equals(metadata.FrameworkVersion, BankingGatewayPluginManager.FrameworkVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Plugin {metadata.BankCode} requires framework {metadata.FrameworkVersion}; current is {BankingGatewayPluginManager.FrameworkVersion}.");
        }
    }
}

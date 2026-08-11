using Gateway.Framework.Core.Abstractions;
using Gateway.Framework.Core.Observability.Correlation;
using Gateway.Framework.Core.Idempotency;
using Gateway.Framework.Plugins.Yarp;
using Gateway.Framework.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Gateway.Framework.Gateway.Yarp;

public static class YarpExtensions
{
    public const string IdempotencyHeaderName = IdempotencyConstants.HeaderName;

    public static IServiceCollection AddGatewayReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CorrelationIdOptions>(configuration.GetSection(CorrelationIdOptions.SectionName));
        services.AddGatewayResilience(configuration);

        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms(builder =>
            {
                builder.AddRequestTransform(async transformContext =>
                {
                    var accessor = transformContext.HttpContext.RequestServices
                        .GetRequiredService<ICorrelationIdAccessor>();
                    var options = transformContext.HttpContext.RequestServices
                        .GetRequiredService<IOptions<CorrelationIdOptions>>().Value;

                    if (!string.IsNullOrWhiteSpace(accessor.CorrelationId))
                    {
                        transformContext.ProxyRequest.Headers.Remove(options.HeaderName);
                        transformContext.ProxyRequest.Headers.TryAddWithoutValidation(
                            options.HeaderName,
                            accessor.CorrelationId);
                    }

                    if (transformContext.HttpContext.Request.Headers.TryGetValue(IdempotencyHeaderName, out var idempotencyKey) &&
                        !string.IsNullOrWhiteSpace(idempotencyKey))
                    {
                        transformContext.ProxyRequest.Headers.Remove(IdempotencyHeaderName);
                        transformContext.ProxyRequest.Headers.TryAddWithoutValidation(
                            IdempotencyHeaderName,
                            idempotencyKey.ToString());
                    }

                    await Task.CompletedTask;
                });
            });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProxyConfigProvider, PluginProxyConfigProvider>());

        return services;
    }
}

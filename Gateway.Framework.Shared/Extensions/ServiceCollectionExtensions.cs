using Gateway.Framework.Core.Abstractions;
using Gateway.Framework.Core.Idempotency;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Framework.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();
        services.AddScoped<ICurrentTenantAccessor, CurrentTenantAccessor>();
        services.AddScoped<IIdempotencyKeyAccessor, IdempotencyKeyAccessor>();
        services.AddSingleton<IFeatureManager, DefaultFeatureManager>();
        return services;
    }
}

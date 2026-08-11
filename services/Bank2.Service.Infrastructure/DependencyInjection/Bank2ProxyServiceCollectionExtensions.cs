using Bank2.Service.Application.Abstractions.External;
using Bank2.Service.Application.Configuration;
using Bank2.Service.Infrastructure.ExternalServices;
using Bank2.Service.Infrastructure.ExternalServices.Authentication;
using Bank2.Service.Infrastructure.ExternalServices.Http;
using Bank2.Service.Infrastructure.HealthChecks;
using Banking.Service.External.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank2.Service.Infrastructure.DependencyInjection;

internal static class Bank2ProxyServiceCollectionExtensions
{
    public static IServiceCollection AddBank2ExternalProxy(this IServiceCollection services, Bank2ProxyOptions proxyOptions)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<ICorrelationContext, HttpCorrelationContext>();
        services.AddHttpClient("Bank2ExternalAuth");

        services.AddSingleton<IBankExternalAuthenticator>(provider =>
            Bank2ExternalAuthenticatorFactory.Create(
                proxyOptions.Authentication,
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<ILoggerFactory>()));

        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddTransient<BankExternalAuthenticationDelegatingHandler>();

        services.AddHttpClient<Bank2ExternalApiClient>((_, client) =>
            {
                client.BaseAddress = new Uri(proxyOptions.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(proxyOptions.TimeoutSeconds);
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddHttpMessageHandler<BankExternalAuthenticationDelegatingHandler>()
            .AddBank2ProxyResilience();

        services.AddHealthChecks()
            .AddCheck<Bank2ExternalBankHealthCheck>("bank2-external-api", tags: ["external"]);

        return services;
    }
}

internal sealed class Bank2ProxyOptionsValidator : IValidateOptions<Bank2ProxyOptions>
{
    public ValidateOptionsResult Validate(string? name, Bank2ProxyOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl) || !Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail("Bank2Proxy:BaseUrl must be an absolute URI when proxy is enabled.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            return ValidateOptionsResult.Fail("Bank2Proxy:TimeoutSeconds must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}

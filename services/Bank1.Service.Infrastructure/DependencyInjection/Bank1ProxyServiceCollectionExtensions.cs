using Bank1.Service.Application.Abstractions.External;
using Bank1.Service.Application.Configuration;
using Bank1.Service.Infrastructure.ExternalServices;
using Bank1.Service.Infrastructure.ExternalServices.Authentication;
using Bank1.Service.Infrastructure.ExternalServices.Http;
using Bank1.Service.Infrastructure.HealthChecks;
using Banking.Service.External.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank1.Service.Infrastructure.DependencyInjection;

internal static class Bank1ProxyServiceCollectionExtensions
{
    public static IServiceCollection AddBank1ExternalProxy(this IServiceCollection services, Bank1ProxyOptions proxyOptions)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<ICorrelationContext, HttpCorrelationContext>();
        services.AddHttpClient("Bank1ExternalAuth");

        services.AddSingleton<IBankExternalAuthenticator>(provider =>
            Bank1ExternalAuthenticatorFactory.Create(
                proxyOptions.Authentication,
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<ILoggerFactory>()));

        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddTransient<BankExternalAuthenticationDelegatingHandler>();

        services.AddHttpClient<Bank1ExternalApiClient>((provider, client) =>
            {
                client.BaseAddress = new Uri(proxyOptions.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(proxyOptions.TimeoutSeconds);
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddHttpMessageHandler<BankExternalAuthenticationDelegatingHandler>()
            .AddBank1ProxyResilience();

        services.AddScoped<IBank1Client, Bank1BankingProxy>();
        services.AddHealthChecks()
            .AddCheck<Bank1ExternalBankHealthCheck>("bank1-external-api", tags: ["external"]);

        return services;
    }
}

internal sealed class Bank1ProxyOptionsValidator : IValidateOptions<Bank1ProxyOptions>
{
    public ValidateOptionsResult Validate(string? name, Bank1ProxyOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl) || !Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail("Bank1Proxy:BaseUrl must be an absolute URI when proxy is enabled.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            return ValidateOptionsResult.Fail("Bank1Proxy:TimeoutSeconds must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}

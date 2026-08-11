using Banking.Service.External.Abstractions;
using Banking.Service.External.Abstractions.Authentication;
using Bank2.Service.Application.Configuration;
using Microsoft.Extensions.Logging;

namespace Bank2.Service.Infrastructure.ExternalServices.Authentication;

internal static class Bank2ExternalAuthenticatorFactory
{
    public static IBankExternalAuthenticator Create(
        Bank2ExternalAuthenticationOptions options,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        return options.Mode?.ToUpperInvariant() switch
        {
            "APIKEY" => new ApiKeyBankExternalAuthenticator(
                options.ApiKeyHeaderName ?? "X-Api-Key",
                options.ApiKey ?? throw new InvalidOperationException("Bank2Proxy:Authentication:ApiKey is required when Mode=ApiKey.")),
            "OAUTH2CLIENTCREDENTIALS" => new OAuth2ClientCredentialsAuthenticator(
                options.TokenEndpoint ?? throw new InvalidOperationException("Bank2Proxy:Authentication:TokenEndpoint is required."),
                options.ClientId ?? throw new InvalidOperationException("Bank2Proxy:Authentication:ClientId is required."),
                options.ClientSecret ?? throw new InvalidOperationException("Bank2Proxy:Authentication:ClientSecret is required."),
                options.Scope,
                httpClientFactory.CreateClient("Bank2ExternalAuth"),
                loggerFactory.CreateLogger<OAuth2ClientCredentialsAuthenticator>()),
            "MUTUALTLS" => throw new InvalidOperationException(
                "MutualTls authentication requires certificate configuration at the host level."),
            _ => new NoneBankExternalAuthenticator()
        };
    }
}

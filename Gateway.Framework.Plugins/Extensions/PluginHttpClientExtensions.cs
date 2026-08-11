using Gateway.Framework.Core.Abstractions;
using Gateway.Framework.Core.Observability.Correlation;
using Gateway.Framework.Plugins.Abstractions;
using Gateway.Framework.Plugins.Context;
using Gateway.Framework.Plugins.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Gateway.Framework.Plugins.Extensions;

public static class PluginHttpClientExtensions
{
    public static IHttpClientBuilder AddBankPluginHttpClient<TClient>(
        this BankingGatewayPluginContext context,
        string clientName,
        Action<HttpClient>? configureClient = null,
        bool financialOperations = false)
        where TClient : class
    {
        var options = context.Configuration.Get<PluginOptions>() ?? new PluginOptions();
        var builder = context.Services.AddHttpClient<TClient>(clientName, client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            configureClient?.Invoke(client);
        });

        builder.AddHttpMessageHandler(sp => new PluginCorrelationIdHandler(
            sp.GetRequiredService<ICorrelationIdAccessor>(),
            sp.GetRequiredService<IOptions<CorrelationIdOptions>>().Value));

        if (financialOperations)
        {
            builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());
        }

        return builder;
    }
}

internal sealed class PluginCorrelationIdHandler : DelegatingHandler
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly CorrelationIdOptions _options;

    public PluginCorrelationIdHandler(ICorrelationIdAccessor correlationIdAccessor, CorrelationIdOptions options)
    {
        _correlationIdAccessor = correlationIdAccessor;
        _options = options;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_correlationIdAccessor.CorrelationId))
        {
            request.Headers.Remove(_options.HeaderName);
            request.Headers.TryAddWithoutValidation(_options.HeaderName, _correlationIdAccessor.CorrelationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

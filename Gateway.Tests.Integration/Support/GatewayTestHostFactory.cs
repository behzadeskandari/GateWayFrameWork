using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Gateway.Tests.Integration.Support;

internal sealed class BankServiceTestHost<TEntryPoint> : IAsyncDisposable
    where TEntryPoint : class
{
    private readonly WebApplicationFactory<TEntryPoint> _factory;

    public BankServiceTestHost()
    {
        HostName = $"{typeof(TEntryPoint).Assembly.GetName().Name!.ToLowerInvariant()}.tests";
        var databaseId = Guid.NewGuid().ToString("N");
        _factory = new WebApplicationFactory<TEntryPoint>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Database:ConnectionString", $"Data Source=bank-test-{databaseId}.db");
            builder.UseSetting("AuditDatabase:ConnectionString", $"Data Source=bank-audit-test-{databaseId}.db");
            builder.UseSetting("Bank2Reconciliation:Enabled", "false");
            builder.UseSetting("Bank2Proxy:Enabled", "false");
        });
        Handler = _factory.Server.CreateHandler();
        BaseUrl = $"http://{HostName}/";
    }

    public string HostName { get; }

    public string BaseUrl { get; }

    public HttpMessageHandler Handler { get; }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();
}

internal sealed class MultiHostRoutingHandler : HttpMessageHandler
{
    private readonly Dictionary<string, HttpMessageHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public void MapHost(string hostName, HttpMessageHandler handler) => _handlers[hostName] = handler;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri ?? throw new InvalidOperationException("Request URI is required.");
        if (!_handlers.TryGetValue(uri.Host, out var handler))
        {
            return new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent($"No test handler mapped for host '{uri.Host}'.")
            };
        }

        using var rewritten = await CloneRequestForTestServerAsync(request, cancellationToken).ConfigureAwait(false);
        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        return await invoker.SendAsync(rewritten, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<HttpRequestMessage> CloneRequestForTestServerAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var rewritten = new HttpRequestMessage(request.Method, new Uri($"http://localhost{request.RequestUri!.PathAndQuery}", UriKind.Absolute))
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            rewritten.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            rewritten.Content = content;
        }

        return rewritten;
    }
}

internal static class GatewayTestHostFactory
{
    public static WebApplicationFactory<Gateway.Host.GatewayHostApplicationMarker> CreateGatewayFactory(
        MultiHostRoutingHandler routingHandler,
        string bank1BaseUrl,
        string bank2BaseUrl)
    {
        return new WebApplicationFactory<Gateway.Host.GatewayHostApplicationMarker>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:Enabled", "false");
            builder.UseSetting("RateLimit:Enabled", "false");
            builder.UseSetting("Plugins:Bank1:Enabled", "true");
            builder.UseSetting("Plugins:Bank1:BaseUrl", bank1BaseUrl);
            builder.UseSetting("Plugins:Bank2:Enabled", "true");
            builder.UseSetting("Plugins:Bank2:BaseUrl", bank2BaseUrl);

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(routingHandler);
                services.AddSingleton<Yarp.ReverseProxy.Forwarder.IForwarderHttpClientFactory, TestForwarderHttpClientFactory>();
                services.Configure<HttpClientFactoryOptions>(options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(httpBuilder =>
                    {
                        httpBuilder.PrimaryHandler = routingHandler;
                    });
                });
            });
        });
    }
}

internal sealed class TestForwarderHttpClientFactory : Yarp.ReverseProxy.Forwarder.IForwarderHttpClientFactory
{
    private readonly MultiHostRoutingHandler _routingHandler;

    public TestForwarderHttpClientFactory(MultiHostRoutingHandler routingHandler) => _routingHandler = routingHandler;

    public HttpMessageInvoker CreateClient(Yarp.ReverseProxy.Forwarder.ForwarderHttpClientContext context) =>
        new(_routingHandler, disposeHandler: false);
}

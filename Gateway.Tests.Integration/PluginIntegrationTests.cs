using System.Net;
using System.Text.Json;
using Gateway.Host;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Gateway.Tests.Integration;

public sealed class PluginIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PluginIntegrationTests(WebApplicationFactory<Program> factory) =>
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:Enabled", "false");
            builder.UseSetting("RateLimit:Enabled", "false");
            builder.UseSetting("Plugins:Bank1:Enabled", "true");
            builder.UseSetting("Plugins:Bank1:BaseUrl", "http://localhost:5201/");
            builder.UseSetting("Plugins:Bank2:Enabled", "true");
            builder.UseSetting("Plugins:Bank2:BaseUrl", "http://localhost:5202/");
        }).CreateClient();

    [Fact]
    public async Task HealthStatus_IncludesPluginInformation()
    {
        var response = await _client.GetAsync("/api/v1/health/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var plugins = document.RootElement
            .GetProperty("data")
            .GetProperty("plugins");

        Assert.True(plugins.GetArrayLength() >= 2);
    }

    [Fact]
    public async Task Bank1Route_IsRegisteredAndRequiresProxy()
    {
        var response = await _client.GetAsync("/api/v1/banks/bank1/accounts/sample");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Bank2PaymentRoute_IsRegistered()
    {
        var response = await _client.GetAsync("/api/v1/banks/bank2/payments/sample");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}

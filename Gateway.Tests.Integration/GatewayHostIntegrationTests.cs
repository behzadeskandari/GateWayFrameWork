using System.Net;
using Gateway.Host;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Gateway.Tests.Integration;

public sealed class GatewayHostIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GatewayHostIntegrationTests(WebApplicationFactory<Program> factory) =>
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:Enabled", "false");
            builder.UseSetting("RateLimit:Enabled", "false");
        }).CreateClient();

    [Fact]
    public async Task LiveHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadyHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthStatus_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/health/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SecureHeaders_ArePresent()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
    }
}

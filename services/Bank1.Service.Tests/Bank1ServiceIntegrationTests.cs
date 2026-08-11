using System.Net;
using Bank1.Service;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Bank1.Service.Tests;

public sealed class Bank1ServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public Bank1ServiceIntegrationTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task LiveHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListAccounts_ReturnsSampleData()
    {
        var response = await _client.GetAsync("/api/accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("acc-1001", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAccountBalance_ReturnsBalance()
    {
        var response = await _client.GetAsync("/api/accounts/acc-1001/balance");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CorrelationId_IsEchoedInResponse()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/accounts");
        request.Headers.Add("X-Correlation-Id", "test-correlation-123");
        var response = await _client.SendAsync(request);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
        Assert.Equal("test-correlation-123", response.Headers.GetValues("X-Correlation-Id").First());
    }
}

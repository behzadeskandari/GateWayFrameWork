using System.Net;
using System.Net.Http.Json;
using System.Text;
using Bank2.Service;
using Bank2.Service.Application.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Bank2.Service.Tests;

public sealed class Bank2ServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public Bank2ServiceIntegrationTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task LiveHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListPayments_ReturnsSampleData()
    {
        var response = await _client.GetAsync("/api/payments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WithIdempotencyKey_ReturnsAccepted()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(new CreatePaymentRequest("acc-1001", "acc-1002", 10m, "USD", "demo"))
        };
        request.Headers.Add("Idempotency-Key", "demo-payment-key-1");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransfer_ReturnsAccepted()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/transfers")
        {
            Content = JsonContent.Create(new CreateTransferRequest("acc-1001", "acc-1002", 25m, "USD", "demo"))
        };
        request.Headers.Add("Idempotency-Key", "demo-transfer-key-1");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}

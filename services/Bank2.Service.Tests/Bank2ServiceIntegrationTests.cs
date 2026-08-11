using System.Net;
using System.Net.Http.Json;
using Bank2.Service;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Contracts.Transfers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Bank2.Service.Tests;

public sealed class Bank2ServiceIntegrationTests : IClassFixture<Bank2WebApplicationFactory>
{
    private readonly HttpClient _client;

    public Bank2ServiceIntegrationTests(Bank2WebApplicationFactory factory) =>
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
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
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
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}

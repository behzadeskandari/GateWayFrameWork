using System.Net;
using System.Net.Http.Json;
using Bank1.Service;
using Bank2.Service;
using Bank2.Service.Application.Models;
using Gateway.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Gateway.Tests.Integration;

public sealed class BankServiceEndToEndTests
{
    [Fact]
    public async Task Gateway_ProxiesBank1Accounts_WithCorrelationId()
    {
        await using var bank1Host = new BankServiceTestHost<Bank1.Service.Program>();
        await using var bank2Host = new BankServiceTestHost<Bank2.Service.Program>();
        await using var gatewayFactory = CreateGatewayFactory(bank1Host, bank2Host);

        using var client = gatewayFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/banks/bank1/accounts");
        request.Headers.Add("X-Correlation-Id", "gateway-bank1-e2e");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("acc-1001", body, StringComparison.Ordinal);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task Gateway_ProxiesBank2Payments_WithIdempotencyKey()
    {
        await using var bank1Host = new BankServiceTestHost<Bank1.Service.Program>();
        await using var bank2Host = new BankServiceTestHost<Bank2.Service.Program>();
        await using var gatewayFactory = CreateGatewayFactory(bank1Host, bank2Host);

        using var client = gatewayFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/banks/bank2/payments")
        {
            Content = JsonContent.Create(new CreatePaymentRequest("acc-1001", "acc-1002", 12.50m, "USD", "gateway-e2e"))
        };
        request.Headers.Add("Idempotency-Key", "gateway-e2e-payment-1");
        request.Headers.Add("X-Correlation-Id", "gateway-bank2-e2e");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Gateway_PluginHealthStatus_ShowsBankPlugins()
    {
        await using var bank1Host = new BankServiceTestHost<Bank1.Service.Program>();
        await using var bank2Host = new BankServiceTestHost<Bank2.Service.Program>();
        await using var gatewayFactory = CreateGatewayFactory(bank1Host, bank2Host);

        using var client = gatewayFactory.CreateClient();
        var response = await client.GetAsync("/api/v1/health/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("BANK1", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BANK2", body, StringComparison.OrdinalIgnoreCase);
    }

    private static WebApplicationFactory<Gateway.Host.GatewayHostApplicationMarker> CreateGatewayFactory(
        BankServiceTestHost<Bank1.Service.Program> bank1Host,
        BankServiceTestHost<Bank2.Service.Program> bank2Host)
    {
        var routingHandler = new MultiHostRoutingHandler();
        routingHandler.MapHost(bank1Host.HostName, bank1Host.Handler);
        routingHandler.MapHost(bank2Host.HostName, bank2Host.Handler);

        return GatewayTestHostFactory.CreateGatewayFactory(
            routingHandler,
            bank1Host.BaseUrl,
            bank2Host.BaseUrl);
    }
}

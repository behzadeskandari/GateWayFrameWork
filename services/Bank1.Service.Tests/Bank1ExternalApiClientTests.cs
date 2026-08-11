using System.Net;
using System.Text;
using System.Text.Json;
using Bank1.Service.Application.Configuration;
using Bank1.Service.Infrastructure.ExternalServices;
using Bank1.Service.Infrastructure.ExternalServices.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bank1.Service.Tests;

public sealed class Bank1ExternalApiClientTests
{
    [Fact]
    public async Task GetAccountAsync_ReturnsMappedAccount_WhenExternalApiSucceeds()
    {
        var responseJson = JsonSerializer.Serialize(new Bank1ExternalAccountResponse
        {
            AccountId = "acc-1001",
            AccountNumber = "acc-1001",
            HolderName = "External Customer",
            Currency = "USD",
            Balance = 500m,
            Status = "Active",
            OpenedAt = DateTimeOffset.UtcNow
        });

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://bank1.example/") };
        var apiClient = new Bank1ExternalApiClient(
            client,
            Options.Create(new Bank1ProxyOptions()),
            NullLogger<Bank1ExternalApiClient>.Instance);

        var account = await apiClient.GetAccountAsync("acc-1001", CancellationToken.None);

        Assert.NotNull(account);
        Assert.Equal("acc-1001", account!.AccountNumber);
    }

    [Fact]
    public async Task GetAccountAsync_ReturnsNull_WhenExternalApiReturns404()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://bank1.example/") };
        var apiClient = new Bank1ExternalApiClient(
            client,
            Options.Create(new Bank1ProxyOptions()),
            NullLogger<Bank1ExternalApiClient>.Instance);

        var account = await apiClient.GetAccountAsync("missing", CancellationToken.None);

        Assert.Null(account);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
            _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));
    }
}

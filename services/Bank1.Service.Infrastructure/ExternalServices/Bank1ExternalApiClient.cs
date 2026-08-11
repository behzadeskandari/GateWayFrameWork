using System.Text.Json;
using Banking.Service.External.Abstractions;
using Banking.Service.External.Abstractions.Http;
using Bank1.Service.Application.Configuration;
using Bank1.Service.Infrastructure.ExternalServices.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank1.Service.Infrastructure.ExternalServices;

public sealed class Bank1ExternalApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly Bank1ProxyOptions _options;
    private readonly ILogger<Bank1ExternalApiClient> _logger;

    public Bank1ExternalApiClient(
        HttpClient httpClient,
        IOptions<Bank1ProxyOptions> options,
        ILogger<Bank1ExternalApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Bank1ExternalAccountResponse?> GetAccountAsync(string accountId, CancellationToken cancellationToken)
    {
        var path = ApplyTemplate(_options.Endpoints.GetAccountTemplate, accountId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await ExternalBankHttpResponseHandler.EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await DeserializeAsync<Bank1ExternalAccountResponse>(response, cancellationToken);
    }

    public async Task<Bank1ExternalBalanceResponse?> GetBalanceAsync(string accountId, CancellationToken cancellationToken)
    {
        var path = ApplyTemplate(_options.Endpoints.GetBalanceTemplate, accountId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await ExternalBankHttpResponseHandler.EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await DeserializeAsync<Bank1ExternalBalanceResponse>(response, cancellationToken);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, string.Empty);
        using var response = await SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "External Bank1 API request timed out.");
            throw new ExternalBankTimeoutException("External Bank1 API request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "External Bank1 API connection failed.");
            throw new ExternalBankUnavailableException("External Bank1 API connection failed.", ex.HResult);
        }
    }

    private static string ApplyTemplate(string template, string accountId) =>
        template.Replace("{accountId}", Uri.EscapeDataString(accountId), StringComparison.Ordinal);

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }
}

internal static class Bank1ExternalResponseMapper
{
    public static Bank1.Service.Domain.Entities.Account MapAccount(Bank1ExternalAccountResponse external)
    {
        return new Bank1.Service.Domain.Entities.Account(
            Guid.NewGuid(),
            new Bank1.Service.Domain.ValueObjects.AccountNumber(external.AccountNumber),
            external.HolderName,
            external.Currency,
            external.Balance,
            Enum.TryParse<Bank1.Service.Domain.Enums.AccountStatus>(external.Status, true, out var status)
                ? status
                : Bank1.Service.Domain.Enums.AccountStatus.Active,
            external.OpenedAt);
    }

    public static Bank1.Service.Application.Abstractions.External.AccountBalanceSnapshot MapBalance(
        Bank1ExternalBalanceResponse external) =>
        new(
            external.AccountId,
            external.Currency,
            external.AvailableBalance,
            external.AsOf);
}

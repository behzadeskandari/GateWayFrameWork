using System.Net.Http.Json;
using System.Text.Json;
using Banking.Service.External.Abstractions;
using Banking.Service.External.Abstractions.Http;
using Bank2.Service.Application.Configuration;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Contracts.Transfers;
using Bank2.Service.Infrastructure.ExternalServices.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank2.Service.Infrastructure.ExternalServices;

public sealed class Bank2ExternalApiClient
{
    private const string IdempotencyKeyHeaderName = "Idempotency-Key";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly Bank2ProxyOptions _options;
    private readonly ILogger<Bank2ExternalApiClient> _logger;

    public Bank2ExternalApiClient(
        HttpClient httpClient,
        IOptions<Bank2ProxyOptions> options,
        ILogger<Bank2ExternalApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Bank2ExternalPaymentResponse> SubmitPaymentAsync(
        CreatePaymentRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var payload = new Bank2ExternalPaymentRequest
        {
            FromAccountId = request.FromAccountId,
            ToAccountId = request.ToAccountId,
            Amount = request.Amount,
            Currency = request.Currency,
            Reference = request.Reference
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.Endpoints.SubmitPaymentTemplate)
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        ApplyIdempotencyKey(httpRequest, idempotencyKey);

        using var response = await SendAsync(httpRequest, cancellationToken);
        await ExternalBankHttpResponseHandler.EnsureSuccessOrThrowAsync(response, cancellationToken);
        return (await DeserializeAsync<Bank2ExternalPaymentResponse>(response, cancellationToken))!;
    }

    public async Task<Bank2ExternalTransferResponse> SubmitTransferAsync(
        CreateTransferRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var payload = new Bank2ExternalTransferRequest
        {
            FromAccountId = request.FromAccountId,
            ToAccountId = request.ToAccountId,
            Amount = request.Amount,
            Currency = request.Currency,
            Reference = request.Reference
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.Endpoints.SubmitTransferTemplate)
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        ApplyIdempotencyKey(httpRequest, idempotencyKey);

        using var response = await SendAsync(httpRequest, cancellationToken);
        await ExternalBankHttpResponseHandler.EnsureSuccessOrThrowAsync(response, cancellationToken);
        return (await DeserializeAsync<Bank2ExternalTransferResponse>(response, cancellationToken))!;
    }

    public async Task<Bank2ExternalPaymentResponse?> GetPaymentStatusAsync(string paymentId, CancellationToken cancellationToken)
    {
        var path = ApplyPaymentTemplate(_options.Endpoints.GetPaymentStatusTemplate, paymentId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await ExternalBankHttpResponseHandler.EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await DeserializeAsync<Bank2ExternalPaymentResponse>(response, cancellationToken);
    }

    public async Task<Bank2ExternalTransferResponse?> GetTransferStatusAsync(string transferId, CancellationToken cancellationToken)
    {
        var path = ApplyTransferTemplate(_options.Endpoints.GetTransferStatusTemplate, transferId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await ExternalBankHttpResponseHandler.EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await DeserializeAsync<Bank2ExternalTransferResponse>(response, cancellationToken);
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
            _logger.LogWarning(ex, "External Bank2 API request timed out.");
            throw new ExternalBankTimeoutException("External Bank2 API request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "External Bank2 API connection failed.");
            throw new ExternalBankUnavailableException("External Bank2 API connection failed.", ex.HResult);
        }
    }

    private static void ApplyIdempotencyKey(HttpRequestMessage request, string? idempotencyKey)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Remove(IdempotencyKeyHeaderName);
            request.Headers.TryAddWithoutValidation(IdempotencyKeyHeaderName, idempotencyKey);
        }
    }

    private static string ApplyPaymentTemplate(string template, string paymentId) =>
        template.Replace("{paymentId}", Uri.EscapeDataString(paymentId), StringComparison.Ordinal);

    private static string ApplyTransferTemplate(string template, string transferId) =>
        template.Replace("{transferId}", Uri.EscapeDataString(transferId), StringComparison.Ordinal);

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }
}

internal static class Bank2ExternalResponseMapper
{
    public static PaymentResponse MapPayment(Bank2ExternalPaymentResponse external) =>
        new(
            external.Id,
            external.Status,
            external.Message,
            external.ProcessedAt,
            external.BankReferenceId ?? external.Id);

    public static TransferResponse MapTransfer(Bank2ExternalTransferResponse external) =>
        new(
            external.Id,
            external.Status,
            external.Message,
            external.ProcessedAt,
            external.BankReferenceId ?? external.Id);
}

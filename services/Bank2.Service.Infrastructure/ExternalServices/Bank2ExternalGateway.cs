using Bank2.Service.Application.Abstractions.External;
using Bank2.Service.Application.Services;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Contracts.Transfers;
using Bank2.Service.Domain.Enums;
using Bank2.Service.Infrastructure.ExternalServices;
using Bank2.Service.Infrastructure.ExternalServices.Models;

namespace Bank2.Service.Infrastructure.ExternalServices;

public sealed class Bank2ExternalGateway : IBank2ExternalGateway
{
    private readonly Bank2ExternalApiClient _externalApiClient;

    public Bank2ExternalGateway(Bank2ExternalApiClient externalApiClient) =>
        _externalApiClient = externalApiClient;

    public async Task<ExternalSubmissionResult> SubmitPaymentAsync(
        CreatePaymentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var response = await _externalApiClient.SubmitPaymentAsync(request, idempotencyKey, cancellationToken);
        return MapPayment(response);
    }

    public async Task<ExternalSubmissionResult> SubmitTransferAsync(
        CreateTransferRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var response = await _externalApiClient.SubmitTransferAsync(request, idempotencyKey, cancellationToken);
        return MapTransfer(response);
    }

    public async Task<ExternalSubmissionResult?> GetPaymentStatusByBankReferenceAsync(
        string bankReferenceId,
        CancellationToken cancellationToken = default)
    {
        var response = await _externalApiClient.GetPaymentStatusAsync(bankReferenceId, cancellationToken);
        return response is null ? null : MapPayment(response);
    }

    public async Task<ExternalSubmissionResult?> GetTransferStatusByBankReferenceAsync(
        string bankReferenceId,
        CancellationToken cancellationToken = default)
    {
        var response = await _externalApiClient.GetTransferStatusAsync(bankReferenceId, cancellationToken);
        return response is null ? null : MapTransfer(response);
    }

    public static ExternalSubmissionResult MapPayment(Bank2ExternalPaymentResponse external)
    {
        var status = TransactionStatusMapper.MapExternalStatus(external.Status);
        return new ExternalSubmissionResult(
            status,
            external.Message,
            external.ProcessedAt,
            ResolveBankReferenceId(external.BankReferenceId, external.Id),
            status == PaymentStatus.Failed ? external.Status : null);
    }

    internal static ExternalSubmissionResult MapTransfer(Bank2ExternalTransferResponse external)
    {
        var status = TransactionStatusMapper.MapExternalStatus(external.Status);
        return new ExternalSubmissionResult(
            status,
            external.Message,
            external.ProcessedAt,
            ResolveBankReferenceId(external.BankReferenceId, external.Id),
            status == PaymentStatus.Failed ? external.Status : null);
    }

    private static string? ResolveBankReferenceId(string? bankReferenceId, string externalId) =>
        !string.IsNullOrWhiteSpace(bankReferenceId) ? bankReferenceId.Trim() : externalId.Trim();
}

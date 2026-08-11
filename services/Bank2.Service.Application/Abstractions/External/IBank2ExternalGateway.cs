using Bank2.Service.Contracts.Payments;
using Bank2.Service.Contracts.Transfers;
using Bank2.Service.Domain.Enums;

namespace Bank2.Service.Application.Abstractions.External;

public sealed record ExternalSubmissionResult(
    PaymentStatus Status,
    string Message,
    DateTimeOffset ProcessedAt,
    string? BankReferenceId = null,
    string? ErrorCode = null);

public interface IBank2ExternalGateway
{
    Task<ExternalSubmissionResult> SubmitPaymentAsync(
        CreatePaymentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<ExternalSubmissionResult> SubmitTransferAsync(
        CreateTransferRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<ExternalSubmissionResult?> GetPaymentStatusByBankReferenceAsync(
        string bankReferenceId,
        CancellationToken cancellationToken = default);

    Task<ExternalSubmissionResult?> GetTransferStatusByBankReferenceAsync(
        string bankReferenceId,
        CancellationToken cancellationToken = default);
}

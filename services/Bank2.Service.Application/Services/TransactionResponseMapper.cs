using Bank2.Service.Contracts.Payments;
using Bank2.Service.Contracts.Transfers;
using Bank2.Service.Domain.Entities;
using Bank2.Service.Domain.Enums;

namespace Bank2.Service.Application.Services;

internal static class TransactionResponseMapper
{
    public static PaymentResponse ToPaymentResponse(Payment payment) =>
        new(
            payment.Id,
            payment.Status.ToString(),
            BuildMessage(payment, "payment"),
            payment.CreatedAt,
            payment.BankReferenceId,
            RequiresStatusInquiry(payment.Status));

    public static TransferResponse ToTransferResponse(Transfer transfer) =>
        new(
            transfer.Id,
            transfer.Status.ToString(),
            BuildMessage(transfer, "transfer"),
            transfer.CreatedAt,
            transfer.BankReferenceId,
            RequiresStatusInquiry(transfer.Status));

    public static bool RequiresStatusInquiry(PaymentStatus status) =>
        status is PaymentStatus.Unknown or PaymentStatus.Pending or PaymentStatus.Submitted;

    private static string BuildMessage(Payment payment, string resourceType) =>
        payment.Status switch
        {
            PaymentStatus.Unknown =>
                $"The {resourceType} outcome is uncertain. Use GET /api/{resourceType}s/{{id}} or wait for reconciliation.",
            PaymentStatus.Failed =>
                string.IsNullOrWhiteSpace(payment.ErrorCode)
                    ? $"The {resourceType} was rejected by the external bank."
                    : $"The {resourceType} was rejected by the external bank ({payment.ErrorCode}).",
            PaymentStatus.Pending or PaymentStatus.Submitted =>
                $"The {resourceType} has been submitted and is awaiting external confirmation.",
            PaymentStatus.Accepted =>
                "The external bank accepted the operation.",
            PaymentStatus.Completed =>
                "The external bank completed the operation.",
            _ => $"Sample {resourceType} accepted for demonstration only. No real funds moved."
        };

    private static string BuildMessage(Transfer transfer, string resourceType) =>
        transfer.Status switch
        {
            PaymentStatus.Unknown =>
                $"The {resourceType} outcome is uncertain. Use GET /api/{resourceType}s/{{id}} or wait for reconciliation.",
            PaymentStatus.Failed =>
                string.IsNullOrWhiteSpace(transfer.ErrorCode)
                    ? $"The {resourceType} was rejected by the external bank."
                    : $"The {resourceType} was rejected by the external bank ({transfer.ErrorCode}).",
            PaymentStatus.Pending or PaymentStatus.Submitted =>
                $"The {resourceType} has been submitted and is awaiting external confirmation.",
            PaymentStatus.Accepted =>
                "The external bank accepted the operation.",
            PaymentStatus.Completed =>
                "The external bank completed the operation.",
            _ => $"Sample {resourceType} accepted for demonstration only. No real funds moved."
        };
}

using Bank2.Service.Domain.Enums;

namespace Bank2.Service.Application.Services;

public static class TransactionStatusMapper
{
    public static PaymentStatus MapExternalStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return PaymentStatus.Unknown;
        }

        return status.Trim().ToUpperInvariant() switch
        {
            "PENDING" => PaymentStatus.Pending,
            "SUBMITTED" => PaymentStatus.Submitted,
            "ACCEPTED" => PaymentStatus.Accepted,
            "COMPLETED" => PaymentStatus.Completed,
            "FAILED" => PaymentStatus.Failed,
            "UNKNOWN" => PaymentStatus.Unknown,
            _ => PaymentStatus.Unknown
        };
    }

    public static bool IsUncertain(PaymentStatus status) =>
        status is PaymentStatus.Unknown or PaymentStatus.Pending or PaymentStatus.Submitted;

    public static bool IsTerminal(PaymentStatus status) =>
        status is PaymentStatus.Completed or PaymentStatus.Failed;
}

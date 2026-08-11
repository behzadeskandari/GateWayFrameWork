namespace Bank2.Service.Application.Abstractions.External;

using Bank2.Service.Contracts.Payments;
using Bank2.Service.Contracts.Transfers;

public interface IBank2Client
{
    Task<PaymentResponse> SubmitPaymentAsync(
        CreatePaymentRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    Task<TransferResponse> SubmitTransferAsync(
        CreateTransferRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse> GetPaymentStatusAsync(string paymentId, CancellationToken cancellationToken = default);

    Task<TransferResponse> GetTransferStatusAsync(string transferId, CancellationToken cancellationToken = default);
}

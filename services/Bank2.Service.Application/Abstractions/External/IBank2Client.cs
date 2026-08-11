using Bank2.Service.Contracts.Payments;
using Bank2.Service.Contracts.Transfers;

namespace Bank2.Service.Application.Abstractions.External;

public interface IBank2Client
{
    Task<PaymentResponse> SubmitPaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<TransferResponse> SubmitTransferAsync(CreateTransferRequest request, CancellationToken cancellationToken = default);
}

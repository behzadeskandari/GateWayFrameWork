using Bank2.Service.Application.Abstractions;
using Bank2.Service.Contracts.Transfers;

namespace Bank2.Service.Application.Features.Transfers.GetTransferStatus;

public interface IGetTransferStatusHandler
{
    Task<TransferResponse> HandleAsync(string transferId, CancellationToken cancellationToken = default);
}

public sealed class GetTransferStatusHandler : IGetTransferStatusHandler
{
    private readonly IFinancialTransactionService _financialTransactionService;

    public GetTransferStatusHandler(IFinancialTransactionService financialTransactionService) =>
        _financialTransactionService = financialTransactionService;

    public Task<TransferResponse> HandleAsync(string transferId, CancellationToken cancellationToken = default) =>
        _financialTransactionService.GetTransferStatusAsync(transferId, cancellationToken);
}

using Bank2.Service.Application.Abstractions;
using Bank2.Service.Application.Features.Transfers.CreateTransfer;
using Bank2.Service.Contracts.Transfers;

namespace Bank2.Service.Application.Features.Transfers.CreateTransfer;

public interface ICreateTransferHandler
{
    Task<TransferResponse> HandleAsync(
        CreateTransferRequest request,
        string? idempotencyKey,
        string? correlationId,
        CancellationToken cancellationToken = default);
}

public sealed class CreateTransferHandler : ICreateTransferHandler
{
    private readonly IFinancialTransactionService _financialTransactionService;

    public CreateTransferHandler(IFinancialTransactionService financialTransactionService) =>
        _financialTransactionService = financialTransactionService;

    public Task<TransferResponse> HandleAsync(
        CreateTransferRequest request,
        string? idempotencyKey,
        string? correlationId,
        CancellationToken cancellationToken = default) =>
        _financialTransactionService.SubmitTransferAsync(request, idempotencyKey, correlationId, cancellationToken);
}

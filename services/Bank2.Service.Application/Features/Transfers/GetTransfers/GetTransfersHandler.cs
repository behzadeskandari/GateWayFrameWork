using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Application.Configuration;
using Bank2.Service.Application.Features.Transfers.GetTransfers;
using Bank2.Service.Contracts.Transfers;
using Bank2.Service.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Bank2.Service.Application.Features.Transfers.GetTransfers;

public interface IGetTransfersHandler
{
    Task<TransfersListResponse> HandleAsync(string? correlationId, CancellationToken cancellationToken = default);
}

public sealed class GetTransfersHandler : IGetTransfersHandler
{
    private readonly ITransferRepository _transferRepository;
    private readonly Bank2Options _options;

    public GetTransfersHandler(ITransferRepository transferRepository, IOptions<Bank2Options> options)
    {
        _transferRepository = transferRepository;
        _options = options.Value;
    }

    public async Task<TransfersListResponse> HandleAsync(string? correlationId, CancellationToken cancellationToken = default)
    {
        var transfers = await _transferRepository.GetAllAsync(cancellationToken);
        var summaries = transfers.Select(MapToSummary).ToList();
        return new TransfersListResponse(summaries, _options.ServiceName, correlationId);
    }

    private static TransferSummaryResponse MapToSummary(Transfer transfer) =>
        new(
            transfer.Id,
            transfer.FromAccountId,
            transfer.Amount,
            transfer.Currency,
            transfer.Status.ToString(),
            transfer.CreatedAt);
}

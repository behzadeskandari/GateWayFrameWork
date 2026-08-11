using System.Text.Json;
using Bank2.Service.Application.Abstractions.External;
using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Contracts.Transfers;
using Bank2.Service.Domain.Entities;
using FluentValidation;

namespace Bank2.Service.Application.Features.Transfers.CreateTransfer;

public interface ICreateTransferHandler
{
    Task<TransferResponse> HandleAsync(
        CreateTransferRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class CreateTransferHandler : ICreateTransferHandler
{
    private const string TransferOperationType = "Transfer";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IBank2Client _bank2Client;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateTransferRequest> _validator;

    public CreateTransferHandler(
        IBank2Client bank2Client,
        IIdempotencyStore idempotencyStore,
        IUnitOfWork unitOfWork,
        IValidator<CreateTransferRequest> validator)
    {
        _bank2Client = bank2Client;
        _idempotencyStore = idempotencyStore;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<TransferResponse> HandleAsync(
        CreateTransferRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _idempotencyStore.GetByKeyAsync(idempotencyKey, cancellationToken);
            if (existing is not null &&
                string.Equals(existing.OperationType, TransferOperationType, StringComparison.Ordinal))
            {
                return JsonSerializer.Deserialize<TransferResponse>(existing.ResponsePayload, JsonOptions)!;
            }
        }

        var response = await _bank2Client.SubmitTransferAsync(request, cancellationToken);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var record = IdempotencyRecord.Create(
                idempotencyKey,
                TransferOperationType,
                JsonSerializer.Serialize(response, JsonOptions));
            await _idempotencyStore.AddAsync(record, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }
}

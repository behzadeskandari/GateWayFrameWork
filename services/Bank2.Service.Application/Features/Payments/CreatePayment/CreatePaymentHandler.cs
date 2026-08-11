using System.Text.Json;
using Bank2.Service.Application.Abstractions.External;
using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Domain.Entities;
using FluentValidation;

namespace Bank2.Service.Application.Features.Payments.CreatePayment;

public interface ICreatePaymentHandler
{
    Task<PaymentResponse> HandleAsync(
        CreatePaymentRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class CreatePaymentHandler : ICreatePaymentHandler
{
    private const string PaymentOperationType = "Payment";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IBank2Client _bank2Client;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePaymentRequest> _validator;

    public CreatePaymentHandler(
        IBank2Client bank2Client,
        IIdempotencyStore idempotencyStore,
        IUnitOfWork unitOfWork,
        IValidator<CreatePaymentRequest> validator)
    {
        _bank2Client = bank2Client;
        _idempotencyStore = idempotencyStore;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaymentResponse> HandleAsync(
        CreatePaymentRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _idempotencyStore.GetByKeyAsync(idempotencyKey, cancellationToken);
            if (existing is not null &&
                string.Equals(existing.OperationType, PaymentOperationType, StringComparison.Ordinal))
            {
                return JsonSerializer.Deserialize<PaymentResponse>(existing.ResponsePayload, JsonOptions)!;
            }
        }

        var response = await _bank2Client.SubmitPaymentAsync(request, cancellationToken);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var record = IdempotencyRecord.Create(
                idempotencyKey,
                PaymentOperationType,
                JsonSerializer.Serialize(response, JsonOptions));
            await _idempotencyStore.AddAsync(record, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }
}

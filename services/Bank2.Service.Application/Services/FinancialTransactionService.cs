using System.Text.Json;
using Banking.Service.External.Abstractions;
using Bank2.Service.Application.Abstractions;
using Bank2.Service.Application.Abstractions.External;
using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Application.Configuration;
using Bank2.Service.Application.Exceptions;
using Bank2.Service.Application.Services;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Contracts.Transfers;
using Bank2.Service.Domain.Entities;
using Bank2.Service.Domain.Enums;
using Bank2.Service.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank2.Service.Application.Services;

public sealed class FinancialTransactionService : IFinancialTransactionService
{
    private const string PaymentOperationType = "Payment";
    private const string TransferOperationType = "Transfer";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPaymentRepository _paymentRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IBank2ExternalGateway? _externalGateway;
    private readonly Bank2ProxyOptions _proxyOptions;
    private readonly IValidator<CreatePaymentRequest> _paymentValidator;
    private readonly IValidator<CreateTransferRequest> _transferValidator;
    private readonly ILogger<FinancialTransactionService> _logger;

    public FinancialTransactionService(
        IPaymentRepository paymentRepository,
        ITransferRepository transferRepository,
        IIdempotencyStore idempotencyStore,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IOptions<Bank2ProxyOptions> proxyOptions,
        IValidator<CreatePaymentRequest> paymentValidator,
        IValidator<CreateTransferRequest> transferValidator,
        ILogger<FinancialTransactionService> logger,
        IBank2ExternalGateway? externalGateway = null)
    {
        _paymentRepository = paymentRepository;
        _transferRepository = transferRepository;
        _idempotencyStore = idempotencyStore;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _externalGateway = externalGateway;
        _proxyOptions = proxyOptions.Value;
        _paymentValidator = paymentValidator;
        _transferValidator = transferValidator;
        _logger = logger;
    }

    public async Task<PaymentResponse> SubmitPaymentAsync(
        CreatePaymentRequest request,
        string? idempotencyKey,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        await _paymentValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (_proxyOptions.Enabled)
        {
            EnsureIdempotencyKey(idempotencyKey);
            return await SubmitPaymentExternalAsync(request, idempotencyKey!, correlationId, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return await SubmitPaymentLocalAsync(request, correlationId, cancellationToken);
        }

        return await SubmitPaymentLocalWithIdempotencyAsync(request, idempotencyKey, correlationId, cancellationToken);
    }

    public async Task<TransferResponse> SubmitTransferAsync(
        CreateTransferRequest request,
        string? idempotencyKey,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        await _transferValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (_proxyOptions.Enabled)
        {
            EnsureIdempotencyKey(idempotencyKey);
            return await SubmitTransferExternalAsync(request, idempotencyKey!, correlationId, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return await SubmitTransferLocalAsync(request, correlationId, cancellationToken);
        }

        return await SubmitTransferLocalWithIdempotencyAsync(request, idempotencyKey, correlationId, cancellationToken);
    }

    public async Task<PaymentResponse> GetPaymentStatusAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            throw new ExternalBankResponseException($"Payment '{paymentId}' was not found.", 404);
        }

        if (_proxyOptions.Enabled &&
            !string.IsNullOrWhiteSpace(payment.BankReferenceId) &&
            TransactionStatusMapper.IsUncertain(payment.Status))
        {
            payment = await RefreshPaymentFromExternalAsync(paymentId, payment.BankReferenceId, payment.CorrelationId, cancellationToken)
                ?? payment;
        }

        return TransactionResponseMapper.ToPaymentResponse(payment);
    }

    public async Task<TransferResponse> GetTransferStatusAsync(string transferId, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdAsync(transferId, cancellationToken);
        if (transfer is null)
        {
            throw new ExternalBankResponseException($"Transfer '{transferId}' was not found.", 404);
        }

        if (_proxyOptions.Enabled &&
            !string.IsNullOrWhiteSpace(transfer.BankReferenceId) &&
            TransactionStatusMapper.IsUncertain(transfer.Status))
        {
            transfer = await RefreshTransferFromExternalAsync(transferId, transfer.BankReferenceId, transfer.CorrelationId, cancellationToken)
                ?? transfer;
        }

        return TransactionResponseMapper.ToTransferResponse(transfer);
    }

    private async Task<PaymentResponse> SubmitPaymentExternalAsync(
        CreatePaymentRequest request,
        string idempotencyKey,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var fingerprint = IdempotencyFingerprint.Compute(request);
        var acquire = await _idempotencyStore.AcquireAsync(
            idempotencyKey,
            PaymentOperationType,
            fingerprint,
            cancellationToken);

        switch (acquire.Result)
        {
            case IdempotencyAcquireResult.Completed:
                return JsonSerializer.Deserialize<PaymentResponse>(acquire.ResponsePayload!, JsonOptions)!;
            case IdempotencyAcquireResult.InProgress:
                throw new IdempotencyInProgressException(
                    $"A request with idempotency key '{idempotencyKey}' is already being processed.");
            case IdempotencyAcquireResult.Conflict:
                throw new IdempotencyConflictException(
                    $"Idempotency key '{idempotencyKey}' cannot be reused with a different request.");
        }

        var paymentId = GeneratePaymentId();
        var money = new Money(request.Amount, request.Currency);
        var payment = Payment.CreateForExternalSubmission(
            paymentId,
            request.FromAccountId,
            request.ToAccountId,
            money,
            request.Reference,
            idempotencyKey,
            correlationId);

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await WritePaymentLifecycleAsync("PaymentCreated", payment, success: true, correlationId, cancellationToken);

        payment.MarkSubmitted();
        await _paymentRepository.UpdateAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await WritePaymentLifecycleAsync("PaymentSubmitted", payment, success: true, correlationId, cancellationToken);

        try
        {
            var external = await RequireExternalGateway().SubmitPaymentAsync(request, idempotencyKey, cancellationToken);
            ApplyExternalResult(payment, external);
            await _paymentRepository.UpdateAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await WritePaymentLifecycleForStatusAsync(payment, correlationId, cancellationToken);

            var response = TransactionResponseMapper.ToPaymentResponse(payment);
            await _idempotencyStore.CompleteAsync(idempotencyKey, Serialize(response), cancellationToken);
            return response;
        }
        catch (ExternalBankTimeoutException ex)
        {
            _logger.LogWarning(ex, "External payment submission timed out for payment {PaymentId}.", paymentId);
            return await FinalizePaymentAsUnknownAsync(payment, idempotencyKey, correlationId, "ExternalBankTimeout", cancellationToken);
        }
        catch (ExternalBankUnavailableException ex)
        {
            _logger.LogWarning(ex, "External payment submission unavailable for payment {PaymentId}.", paymentId);
            return await FinalizePaymentAsUnknownAsync(payment, idempotencyKey, correlationId, "ExternalBankUnavailable", cancellationToken);
        }
        catch (ExternalBankAuthenticationException)
        {
            return await FinalizePaymentAsFailedAsync(payment, idempotencyKey, correlationId, "ExternalBankAuthenticationFailed", cancellationToken);
        }
        catch (ExternalBankResponseException ex) when (IsDefinitiveFailure(ex))
        {
            return await FinalizePaymentAsFailedAsync(payment, idempotencyKey, correlationId, ex.ErrorCode ?? "ExternalBankError", cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "External payment response was malformed for payment {PaymentId}.", paymentId);
            return await FinalizePaymentAsUnknownAsync(payment, idempotencyKey, correlationId, "MalformedExternalResponse", cancellationToken);
        }
    }

    private async Task<TransferResponse> SubmitTransferExternalAsync(
        CreateTransferRequest request,
        string idempotencyKey,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var fingerprint = IdempotencyFingerprint.Compute(request);
        var acquire = await _idempotencyStore.AcquireAsync(
            idempotencyKey,
            TransferOperationType,
            fingerprint,
            cancellationToken);

        switch (acquire.Result)
        {
            case IdempotencyAcquireResult.Completed:
                return JsonSerializer.Deserialize<TransferResponse>(acquire.ResponsePayload!, JsonOptions)!;
            case IdempotencyAcquireResult.InProgress:
                throw new IdempotencyInProgressException(
                    $"A request with idempotency key '{idempotencyKey}' is already being processed.");
            case IdempotencyAcquireResult.Conflict:
                throw new IdempotencyConflictException(
                    $"Idempotency key '{idempotencyKey}' cannot be reused with a different request.");
        }

        var transferId = GenerateTransferId();
        var money = new Money(request.Amount, request.Currency);
        var transfer = Transfer.CreateForExternalSubmission(
            transferId,
            request.FromAccountId,
            request.ToAccountId,
            money,
            request.Reference,
            idempotencyKey,
            correlationId);

        await _transferRepository.AddAsync(transfer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await WriteTransferLifecycleAsync("TransferCreated", transfer, success: true, correlationId, cancellationToken);

        transfer.MarkSubmitted();
        await _transferRepository.UpdateAsync(transfer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await WriteTransferLifecycleAsync("TransferSubmitted", transfer, success: true, correlationId, cancellationToken);

        try
        {
            var external = await RequireExternalGateway().SubmitTransferAsync(request, idempotencyKey, cancellationToken);
            ApplyExternalResult(transfer, external);
            await _transferRepository.UpdateAsync(transfer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await WriteTransferLifecycleForStatusAsync(transfer, correlationId, cancellationToken);

            var response = TransactionResponseMapper.ToTransferResponse(transfer);
            await _idempotencyStore.CompleteAsync(idempotencyKey, Serialize(response), cancellationToken);
            return response;
        }
        catch (ExternalBankTimeoutException ex)
        {
            _logger.LogWarning(ex, "External transfer submission timed out for transfer {TransferId}.", transferId);
            return await FinalizeTransferAsUnknownAsync(transfer, idempotencyKey, correlationId, "ExternalBankTimeout", cancellationToken);
        }
        catch (ExternalBankUnavailableException ex)
        {
            _logger.LogWarning(ex, "External transfer submission unavailable for transfer {TransferId}.", transferId);
            return await FinalizeTransferAsUnknownAsync(transfer, idempotencyKey, correlationId, "ExternalBankUnavailable", cancellationToken);
        }
        catch (ExternalBankAuthenticationException)
        {
            return await FinalizeTransferAsFailedAsync(transfer, idempotencyKey, correlationId, "ExternalBankAuthenticationFailed", cancellationToken);
        }
        catch (ExternalBankResponseException ex) when (IsDefinitiveFailure(ex))
        {
            return await FinalizeTransferAsFailedAsync(transfer, idempotencyKey, correlationId, ex.ErrorCode ?? "ExternalBankError", cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "External transfer response was malformed for transfer {TransferId}.", transferId);
            return await FinalizeTransferAsUnknownAsync(transfer, idempotencyKey, correlationId, "MalformedExternalResponse", cancellationToken);
        }
    }

    private async Task<PaymentResponse> SubmitPaymentLocalAsync(
        CreatePaymentRequest request,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var money = new Money(request.Amount, request.Currency);
        var payment = Payment.Create(
            GeneratePaymentId(),
            request.FromAccountId,
            request.ToAccountId,
            money,
            request.Reference,
            PaymentStatus.Accepted,
            correlationId: correlationId);

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TransactionResponseMapper.ToPaymentResponse(payment);
    }

    private async Task<PaymentResponse> SubmitPaymentLocalWithIdempotencyAsync(
        CreatePaymentRequest request,
        string idempotencyKey,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var fingerprint = IdempotencyFingerprint.Compute(request);
        var acquire = await _idempotencyStore.AcquireAsync(
            idempotencyKey,
            PaymentOperationType,
            fingerprint,
            cancellationToken);

        switch (acquire.Result)
        {
            case IdempotencyAcquireResult.Completed:
                return JsonSerializer.Deserialize<PaymentResponse>(acquire.ResponsePayload!, JsonOptions)!;
            case IdempotencyAcquireResult.InProgress:
                throw new IdempotencyInProgressException(
                    $"A request with idempotency key '{idempotencyKey}' is already being processed.");
            case IdempotencyAcquireResult.Conflict:
                throw new IdempotencyConflictException(
                    $"Idempotency key '{idempotencyKey}' cannot be reused with a different request.");
        }

        var response = await SubmitPaymentLocalAsync(request, correlationId, cancellationToken);
        await _idempotencyStore.CompleteAsync(idempotencyKey, Serialize(response), cancellationToken);
        return response;
    }

    private async Task<TransferResponse> SubmitTransferLocalAsync(
        CreateTransferRequest request,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var money = new Money(request.Amount, request.Currency);
        var transfer = Transfer.Create(
            GenerateTransferId(),
            request.FromAccountId,
            request.ToAccountId,
            money,
            request.Reference,
            PaymentStatus.Accepted,
            correlationId: correlationId);

        await _transferRepository.AddAsync(transfer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TransactionResponseMapper.ToTransferResponse(transfer);
    }

    private async Task<TransferResponse> SubmitTransferLocalWithIdempotencyAsync(
        CreateTransferRequest request,
        string idempotencyKey,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var fingerprint = IdempotencyFingerprint.Compute(request);
        var acquire = await _idempotencyStore.AcquireAsync(
            idempotencyKey,
            TransferOperationType,
            fingerprint,
            cancellationToken);

        switch (acquire.Result)
        {
            case IdempotencyAcquireResult.Completed:
                return JsonSerializer.Deserialize<TransferResponse>(acquire.ResponsePayload!, JsonOptions)!;
            case IdempotencyAcquireResult.InProgress:
                throw new IdempotencyInProgressException(
                    $"A request with idempotency key '{idempotencyKey}' is already being processed.");
            case IdempotencyAcquireResult.Conflict:
                throw new IdempotencyConflictException(
                    $"Idempotency key '{idempotencyKey}' cannot be reused with a different request.");
        }

        var response = await SubmitTransferLocalAsync(request, correlationId, cancellationToken);
        await _idempotencyStore.CompleteAsync(idempotencyKey, Serialize(response), cancellationToken);
        return response;
    }

    private async Task<Payment?> RefreshPaymentFromExternalAsync(
        string paymentId,
        string bankReferenceId,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var external = await RequireExternalGateway().GetPaymentStatusByBankReferenceAsync(bankReferenceId, cancellationToken);
            if (external is null)
            {
                return null;
            }

            var payment = await _paymentRepository.GetByIdForUpdateAsync(paymentId, cancellationToken);
            if (payment is null || TransactionStatusMapper.IsTerminal(payment.Status))
            {
                return payment;
            }

            ApplyExternalResult(payment, external);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return payment;
        }
        catch (Exception ex) when (ex is ExternalBankUnavailableException or ExternalBankTimeoutException)
        {
            _logger.LogWarning(ex, "Unable to refresh payment {PaymentId} from external bank.", paymentId);
            return null;
        }
    }

    private async Task<Transfer?> RefreshTransferFromExternalAsync(
        string transferId,
        string bankReferenceId,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var external = await RequireExternalGateway().GetTransferStatusByBankReferenceAsync(bankReferenceId, cancellationToken);
            if (external is null)
            {
                return null;
            }

            var transfer = await _transferRepository.GetByIdForUpdateAsync(transferId, cancellationToken);
            if (transfer is null || TransactionStatusMapper.IsTerminal(transfer.Status))
            {
                return transfer;
            }

            ApplyExternalResult(transfer, external);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return transfer;
        }
        catch (Exception ex) when (ex is ExternalBankUnavailableException or ExternalBankTimeoutException)
        {
            _logger.LogWarning(ex, "Unable to refresh transfer {TransferId} from external bank.", transferId);
            return null;
        }
    }

    private async Task<PaymentResponse> FinalizePaymentAsUnknownAsync(
        Payment payment,
        string idempotencyKey,
        string? correlationId,
        string errorCode,
        CancellationToken cancellationToken)
    {
        payment.MarkUnknown();
        await _paymentRepository.UpdateAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await WritePaymentLifecycleAsync("PaymentUnknown", payment, success: false, correlationId, cancellationToken, errorCode);

        var response = TransactionResponseMapper.ToPaymentResponse(payment);
        await _idempotencyStore.CompleteAsync(idempotencyKey, Serialize(response), cancellationToken);
        return response;
    }

    private async Task<PaymentResponse> FinalizePaymentAsFailedAsync(
        Payment payment,
        string idempotencyKey,
        string? correlationId,
        string errorCode,
        CancellationToken cancellationToken)
    {
        payment.MarkFailed(errorCode);
        await _paymentRepository.UpdateAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await WritePaymentLifecycleAsync("PaymentFailed", payment, success: false, correlationId, cancellationToken, errorCode);

        var response = TransactionResponseMapper.ToPaymentResponse(payment);
        await _idempotencyStore.CompleteAsync(idempotencyKey, Serialize(response), cancellationToken);
        return response;
    }

    private async Task<TransferResponse> FinalizeTransferAsUnknownAsync(
        Transfer transfer,
        string idempotencyKey,
        string? correlationId,
        string errorCode,
        CancellationToken cancellationToken)
    {
        transfer.MarkUnknown();
        await _transferRepository.UpdateAsync(transfer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await WriteTransferLifecycleAsync("TransferUnknown", transfer, success: false, correlationId, cancellationToken, errorCode);

        var response = TransactionResponseMapper.ToTransferResponse(transfer);
        await _idempotencyStore.CompleteAsync(idempotencyKey, Serialize(response), cancellationToken);
        return response;
    }

    private async Task<TransferResponse> FinalizeTransferAsFailedAsync(
        Transfer transfer,
        string idempotencyKey,
        string? correlationId,
        string errorCode,
        CancellationToken cancellationToken)
    {
        transfer.MarkFailed(errorCode);
        await _transferRepository.UpdateAsync(transfer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await WriteTransferLifecycleAsync("TransferFailed", transfer, success: false, correlationId, cancellationToken, errorCode);

        var response = TransactionResponseMapper.ToTransferResponse(transfer);
        await _idempotencyStore.CompleteAsync(idempotencyKey, Serialize(response), cancellationToken);
        return response;
    }

    private static void ApplyExternalResult(Payment payment, ExternalSubmissionResult external) =>
        payment.ApplyExternalStatus(external.Status, external.BankReferenceId, external.ErrorCode);

    private static void ApplyExternalResult(Transfer transfer, ExternalSubmissionResult external) =>
        transfer.ApplyExternalStatus(external.Status, external.BankReferenceId, external.ErrorCode);

    private async Task WritePaymentLifecycleForStatusAsync(
        Payment payment,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var operation = payment.Status switch
        {
            PaymentStatus.Accepted => "PaymentAccepted",
            PaymentStatus.Completed => "PaymentCompleted",
            PaymentStatus.Failed => "PaymentFailed",
            PaymentStatus.Unknown => "PaymentUnknown",
            _ => "PaymentSubmitted"
        };

        await WritePaymentLifecycleAsync(operation, payment, payment.Status != PaymentStatus.Failed, correlationId, cancellationToken, payment.ErrorCode);
    }

    private async Task WriteTransferLifecycleForStatusAsync(
        Transfer transfer,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var operation = transfer.Status switch
        {
            PaymentStatus.Accepted => "TransferAccepted",
            PaymentStatus.Completed => "TransferCompleted",
            PaymentStatus.Failed => "TransferFailed",
            PaymentStatus.Unknown => "TransferUnknown",
            _ => "TransferSubmitted"
        };

        await WriteTransferLifecycleAsync(operation, transfer, transfer.Status != PaymentStatus.Failed, correlationId, cancellationToken, transfer.ErrorCode);
    }

    private Task WritePaymentLifecycleAsync(
        string operation,
        Payment payment,
        bool success,
        string? correlationId,
        CancellationToken cancellationToken,
        string? errorCode = null) =>
        _auditService.WriteLifecycleEventAsync(
            operation,
            "Payment",
            payment.Id,
            success,
            correlationId,
            errorCode,
            BuildLifecycleMetadata(payment.BankReferenceId, payment.Status),
            cancellationToken);

    private Task WriteTransferLifecycleAsync(
        string operation,
        Transfer transfer,
        bool success,
        string? correlationId,
        CancellationToken cancellationToken,
        string? errorCode = null) =>
        _auditService.WriteLifecycleEventAsync(
            operation,
            "Transfer",
            transfer.Id,
            success,
            correlationId,
            errorCode,
            BuildLifecycleMetadata(transfer.BankReferenceId, transfer.Status),
            cancellationToken);

    private static string BuildLifecycleMetadata(string? bankReferenceId, PaymentStatus status) =>
        JsonSerializer.Serialize(new
        {
            bankReferenceId,
            status = status.ToString()
        }, JsonOptions);

    private IBank2ExternalGateway RequireExternalGateway() =>
        _externalGateway ?? throw new InvalidOperationException("External Bank2 gateway is not configured.");

    private static void EnsureIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new IdempotencyKeyRequiredException();
        }
    }

    private static bool IsDefinitiveFailure(ExternalBankResponseException exception) =>
        exception.StatusCode is 400 or 409 or 422;

    private static string GeneratePaymentId() => $"pay-{Guid.NewGuid():N}"[..12];

    private static string GenerateTransferId() => $"trf-{Guid.NewGuid():N}"[..12];

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
}

using Banking.Service.External.Abstractions;
using Bank2.Service.Application.Abstractions.External;
using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Application.Configuration;
using Bank2.Service.Contracts.Payments;
using Bank2.Service.Contracts.Transfers;
using Bank2.Service.Domain.Entities;
using Bank2.Service.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Bank2.Service.Infrastructure.ExternalServices;

public sealed class Bank2BankingProxy : IBank2Client
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly Bank2ExternalApiClient? _externalApiClient;
    private readonly Bank2ProxyOptions _options;

    public Bank2BankingProxy(
        IPaymentRepository paymentRepository,
        ITransferRepository transferRepository,
        IOptions<Bank2ProxyOptions> options,
        Bank2ExternalApiClient? externalApiClient = null)
    {
        _paymentRepository = paymentRepository;
        _transferRepository = transferRepository;
        _options = options.Value;
        _externalApiClient = externalApiClient;
    }

    public Task<PaymentResponse> SubmitPaymentAsync(
        CreatePaymentRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            var money = new Money(request.Amount, request.Currency);
            var id = $"pay-{Guid.NewGuid():N}"[..12];
            var payment = Payment.Create(id, request.FromAccountId, request.ToAccountId, money, request.Reference);
            return SubmitLocalPaymentAsync(payment, cancellationToken);
        }

        return SubmitPaymentToExternalBankAsync(request, idempotencyKey, cancellationToken);
    }

    public Task<TransferResponse> SubmitTransferAsync(
        CreateTransferRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            var money = new Money(request.Amount, request.Currency);
            var id = $"trf-{Guid.NewGuid():N}"[..12];
            var transfer = Transfer.Create(id, request.FromAccountId, request.ToAccountId, money, request.Reference);
            return SubmitLocalTransferAsync(transfer, cancellationToken);
        }

        return SubmitTransferToExternalBankAsync(request, idempotencyKey, cancellationToken);
    }

    public Task<PaymentResponse> GetPaymentStatusAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return GetLocalPaymentStatusAsync(paymentId, cancellationToken);
        }

        return GetExternalPaymentStatusAsync(paymentId, cancellationToken);
    }

    public Task<TransferResponse> GetTransferStatusAsync(string transferId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return GetLocalTransferStatusAsync(transferId, cancellationToken);
        }

        return GetExternalTransferStatusAsync(transferId, cancellationToken);
    }

    private async Task<PaymentResponse> SubmitLocalPaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        await _paymentRepository.AddAsync(payment, cancellationToken);
        return new PaymentResponse(
            payment.Id,
            payment.Status.ToString(),
            "Sample payment accepted for demonstration only. No real funds moved.",
            payment.CreatedAt);
    }

    private async Task<TransferResponse> SubmitLocalTransferAsync(Transfer transfer, CancellationToken cancellationToken)
    {
        await _transferRepository.AddAsync(transfer, cancellationToken);
        return new TransferResponse(
            transfer.Id,
            transfer.Status.ToString(),
            "Sample transfer accepted for demonstration only. No real funds moved.",
            transfer.CreatedAt);
    }

    private async Task<PaymentResponse> GetLocalPaymentStatusAsync(string paymentId, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            throw new ExternalBankResponseException(
                $"Payment '{paymentId}' was not found.",
                404);
        }

        return new PaymentResponse(
            payment.Id,
            payment.Status.ToString(),
            "Sample payment status for demonstration only.",
            payment.CreatedAt);
    }

    private async Task<TransferResponse> GetLocalTransferStatusAsync(string transferId, CancellationToken cancellationToken)
    {
        var transfer = await _transferRepository.GetByIdAsync(transferId, cancellationToken);
        if (transfer is null)
        {
            throw new ExternalBankResponseException(
                $"Transfer '{transferId}' was not found.",
                404);
        }

        return new TransferResponse(
            transfer.Id,
            transfer.Status.ToString(),
            "Sample transfer status for demonstration only.",
            transfer.CreatedAt);
    }

    private async Task<PaymentResponse> SubmitPaymentToExternalBankAsync(
        CreatePaymentRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (_externalApiClient is null)
        {
            throw new InvalidOperationException("External Bank2 API client is not configured.");
        }

        var external = await _externalApiClient.SubmitPaymentAsync(request, idempotencyKey, cancellationToken);
        return Bank2ExternalResponseMapper.MapPayment(external);
    }

    private async Task<TransferResponse> SubmitTransferToExternalBankAsync(
        CreateTransferRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (_externalApiClient is null)
        {
            throw new InvalidOperationException("External Bank2 API client is not configured.");
        }

        var external = await _externalApiClient.SubmitTransferAsync(request, idempotencyKey, cancellationToken);
        return Bank2ExternalResponseMapper.MapTransfer(external);
    }

    private async Task<PaymentResponse> GetExternalPaymentStatusAsync(string paymentId, CancellationToken cancellationToken)
    {
        if (_externalApiClient is null)
        {
            throw new InvalidOperationException("External Bank2 API client is not configured.");
        }

        var external = await _externalApiClient.GetPaymentStatusAsync(paymentId, cancellationToken);
        if (external is null)
        {
            throw new ExternalBankResponseException(
                $"Payment '{paymentId}' was not found.",
                404);
        }

        return Bank2ExternalResponseMapper.MapPayment(external);
    }

    private async Task<TransferResponse> GetExternalTransferStatusAsync(string transferId, CancellationToken cancellationToken)
    {
        if (_externalApiClient is null)
        {
            throw new InvalidOperationException("External Bank2 API client is not configured.");
        }

        var external = await _externalApiClient.GetTransferStatusAsync(transferId, cancellationToken);
        if (external is null)
        {
            throw new ExternalBankResponseException(
                $"Transfer '{transferId}' was not found.",
                404);
        }

        return Bank2ExternalResponseMapper.MapTransfer(external);
    }
}

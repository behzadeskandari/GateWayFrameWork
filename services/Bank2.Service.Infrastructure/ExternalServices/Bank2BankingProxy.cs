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
    private readonly Bank2ProxyOptions _options;
    private readonly HttpClient? _httpClient;

    public Bank2BankingProxy(
        IPaymentRepository paymentRepository,
        ITransferRepository transferRepository,
        IOptions<Bank2ProxyOptions> options,
        HttpClient? httpClient = null)
    {
        _paymentRepository = paymentRepository;
        _transferRepository = transferRepository;
        _options = options.Value;
        _httpClient = httpClient;
    }

    public Task<PaymentResponse> SubmitPaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            var money = new Money(request.Amount, request.Currency);
            var id = $"pay-{Guid.NewGuid():N}"[..12];
            var payment = Payment.Create(id, request.FromAccountId, request.ToAccountId, money, request.Reference);
            return SubmitLocalPaymentAsync(payment, cancellationToken);
        }

        return SubmitPaymentToExternalBankAsync(request, cancellationToken);
    }

    public Task<TransferResponse> SubmitTransferAsync(CreateTransferRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            var money = new Money(request.Amount, request.Currency);
            var id = $"trf-{Guid.NewGuid():N}"[..12];
            var transfer = Transfer.Create(id, request.FromAccountId, request.ToAccountId, money, request.Reference);
            return SubmitLocalTransferAsync(transfer, cancellationToken);
        }

        return SubmitTransferToExternalBankAsync(request, cancellationToken);
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

    private Task<PaymentResponse> SubmitPaymentToExternalBankAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (_httpClient is null)
        {
            throw new InvalidOperationException("REQUIRES REAL BANK INTEGRATION");
        }

        _ = request;
        _ = cancellationToken;
        throw new NotImplementedException("REQUIRES REAL BANK INTEGRATION");
    }

    private Task<TransferResponse> SubmitTransferToExternalBankAsync(
        CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        if (_httpClient is null)
        {
            throw new InvalidOperationException("REQUIRES REAL BANK INTEGRATION");
        }

        _ = request;
        _ = cancellationToken;
        throw new NotImplementedException("REQUIRES REAL BANK INTEGRATION");
    }
}

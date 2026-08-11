namespace Bank2.Service.Infrastructure.ExternalServices.Models;

public sealed class Bank2ExternalPaymentRequest
{
    public string FromAccountId { get; init; } = null!;
    public string ToAccountId { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = null!;
    public string? Reference { get; init; }
}

public sealed class Bank2ExternalPaymentResponse
{
    public string Id { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string Message { get; init; } = null!;
    public DateTimeOffset ProcessedAt { get; init; }
    public string? BankReferenceId { get; init; }
}

public sealed class Bank2ExternalTransferRequest
{
    public string FromAccountId { get; init; } = null!;
    public string ToAccountId { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = null!;
    public string? Reference { get; init; }
}

public sealed class Bank2ExternalTransferResponse
{
    public string Id { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string Message { get; init; } = null!;
    public DateTimeOffset ProcessedAt { get; init; }
    public string? BankReferenceId { get; init; }
}

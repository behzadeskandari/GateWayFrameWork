namespace Bank2.Service.Application.Models;

public sealed record PaymentSummary(string Id, string FromAccountId, decimal Amount, string Currency, string Status, DateTimeOffset CreatedAt);

public sealed record CreatePaymentRequest(string FromAccountId, string ToAccountId, decimal Amount, string Currency, string? Reference);

public sealed record PaymentResult(string Id, string Status, string Message, DateTimeOffset ProcessedAt);

public sealed record CreateTransferRequest(string FromAccountId, string ToAccountId, decimal Amount, string Currency, string? Reference);

public sealed record TransferResult(string Id, string Status, string Message, DateTimeOffset ProcessedAt);

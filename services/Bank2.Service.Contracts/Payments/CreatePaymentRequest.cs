namespace Bank2.Service.Contracts.Payments;

public sealed record CreatePaymentRequest(
    string FromAccountId,
    string ToAccountId,
    decimal Amount,
    string Currency,
    string? Reference);

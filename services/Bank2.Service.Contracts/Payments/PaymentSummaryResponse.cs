namespace Bank2.Service.Contracts.Payments;

public sealed record PaymentSummaryResponse(
    string Id,
    string FromAccountId,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt);

namespace Bank2.Service.Contracts.Payments;

public sealed record PaymentResponse(
    string Id,
    string Status,
    string Message,
    DateTimeOffset ProcessedAt);

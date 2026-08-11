namespace Bank2.Service.Contracts.Transfers;

public sealed record TransferResponse(
    string Id,
    string Status,
    string Message,
    DateTimeOffset ProcessedAt);

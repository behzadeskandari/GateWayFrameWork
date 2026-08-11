namespace Bank2.Service.Contracts.Transfers;

public sealed record TransferSummaryResponse(
    string Id,
    string FromAccountId,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt);

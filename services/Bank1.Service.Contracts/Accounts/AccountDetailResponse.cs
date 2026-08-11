namespace Bank1.Service.Contracts.Accounts;

public sealed record AccountDetailResponse(
    string Id,
    string HolderName,
    string Currency,
    string Status,
    DateTimeOffset OpenedAt);

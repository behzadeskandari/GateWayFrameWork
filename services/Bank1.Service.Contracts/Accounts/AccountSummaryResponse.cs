namespace Bank1.Service.Contracts.Accounts;

public sealed record AccountSummaryResponse(
    string Id,
    string HolderName,
    string Currency,
    decimal Balance);

namespace Bank1.Service.Contracts.Accounts;

public sealed record AccountsListResponse(
    IReadOnlyList<AccountSummaryResponse> Data,
    string Service,
    string? CorrelationId);

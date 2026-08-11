namespace Bank2.Service.Contracts.Transfers;

public sealed record CreateTransferRequest(
    string FromAccountId,
    string ToAccountId,
    decimal Amount,
    string Currency,
    string? Reference);

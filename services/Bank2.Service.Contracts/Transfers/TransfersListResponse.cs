namespace Bank2.Service.Contracts.Transfers;

public sealed record TransfersListResponse(
    IReadOnlyList<TransferSummaryResponse> Data,
    string Service,
    string? CorrelationId);

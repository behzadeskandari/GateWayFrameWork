namespace Bank2.Service.Contracts.Payments;

public sealed record PaymentsListResponse(
    IReadOnlyList<PaymentSummaryResponse> Data,
    string Service,
    string? CorrelationId);

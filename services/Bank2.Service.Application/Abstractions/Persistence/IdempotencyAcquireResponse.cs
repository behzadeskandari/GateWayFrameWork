namespace Bank2.Service.Application.Abstractions.Persistence;

public enum IdempotencyAcquireResult
{
    Acquired,
    Completed,
    InProgress,
    Conflict
}

public sealed class IdempotencyAcquireResponse
{
    public IdempotencyAcquireResult Result { get; init; }
    public string? ResponsePayload { get; init; }
}

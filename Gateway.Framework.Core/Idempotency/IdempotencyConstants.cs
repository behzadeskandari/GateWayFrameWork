namespace Gateway.Framework.Core.Idempotency;

/// <summary>
/// The gateway propagates idempotency keys to downstream services.
/// Idempotency enforcement is delegated to downstream banking services.
/// </summary>
public static class IdempotencyConstants
{
    public const string HeaderName = "Idempotency-Key";
}

public interface IIdempotencyKeyAccessor
{
    string? Key { get; set; }
}

public sealed class IdempotencyKeyAccessor : IIdempotencyKeyAccessor
{
    public string? Key { get; set; }
}

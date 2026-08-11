namespace Gateway.Framework.Core.Abstractions;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public string? CorrelationId { get; set; }
}

public sealed class CurrentTenantAccessor : ICurrentTenantAccessor
{
    public string? TenantId { get; set; }
}

public sealed class DefaultFeatureManager : IFeatureManager
{
    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}

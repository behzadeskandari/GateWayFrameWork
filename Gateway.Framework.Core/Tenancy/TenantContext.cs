namespace Gateway.Framework.Core.Tenancy;

public sealed class TenantContext
{
    public string? TenantId { get; init; }
    public string? TenantName { get; init; }
}

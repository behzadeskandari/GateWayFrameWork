namespace Gateway.Framework.Core.Abstractions;

public interface ICurrentTenantAccessor
{
    string? TenantId { get; set; }
}

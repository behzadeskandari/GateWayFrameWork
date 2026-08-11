namespace Gateway.Framework.Core.Abstractions;

public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; set; }
}

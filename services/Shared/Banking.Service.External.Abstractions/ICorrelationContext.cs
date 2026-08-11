namespace Banking.Service.External.Abstractions;

public interface ICorrelationContext
{
    string? CorrelationId { get; }
}

namespace Gateway.Framework.Core.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

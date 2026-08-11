namespace Gateway.Framework.Core.Abstractions;

public interface IFeatureManager
{
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);
}

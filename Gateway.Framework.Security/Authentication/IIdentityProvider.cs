namespace Gateway.Framework.Security.Authentication;

/// <summary>
/// Describes the external OpenID Connect / OAuth2 identity provider used by the gateway.
/// The gateway validates tokens; it does not issue them.
/// </summary>
public interface IIdentityProvider
{
    bool IsConfigured { get; }
    string Authority { get; }
    string Audience { get; }
    IReadOnlyCollection<string> RequiredScopes { get; }
}

public sealed class ExternalIdentityProvider : IIdentityProvider
{
    private readonly AuthOptions _options;

    public ExternalIdentityProvider(Microsoft.Extensions.Options.IOptions<AuthOptions> options) =>
        _options = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Authority) &&
        !string.IsNullOrWhiteSpace(_options.Audience);

    public string Authority => _options.Authority;
    public string Audience => _options.Audience;
    public IReadOnlyCollection<string> RequiredScopes => _options.RequiredScopes;
}

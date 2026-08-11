namespace Gateway.Framework.Security.TokenValidation;

/// <summary>
/// Marker abstraction for JWT bearer token validation performed by the ASP.NET Core authentication middleware.
/// </summary>
public interface ITokenValidator
{
    bool IsEnabled { get; }
}

public sealed class JwtBearerTokenValidator : ITokenValidator
{
    private readonly Authentication.AuthOptions _options;

    public JwtBearerTokenValidator(Microsoft.Extensions.Options.IOptions<Authentication.AuthOptions> options) =>
        _options = options.Value;

    public bool IsEnabled => _options.Enabled;
}

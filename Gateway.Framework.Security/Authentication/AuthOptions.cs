using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Gateway.Framework.Security.Authentication;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// When false, JWT validation middleware is not registered and proxied routes are anonymous
    /// unless explicitly protected elsewhere. Must remain false in Development unless testing auth.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Explicit Development-only bypass. Ignored outside Development environment.
    /// Cannot be enabled in Production.
    /// </summary>
    public bool AllowDevelopmentAnonymous { get; set; }

    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string[] RequiredScopes { get; set; } = ["gateway.access"];
    public bool ValidateScopes { get; set; } = true;
    public bool RequireHttpsMetadata { get; set; } = true;
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
}

public sealed class AuthOptionsValidator : IValidateOptions<AuthOptions>
{
    private readonly IHostEnvironment _environment;

    public AuthOptionsValidator(IHostEnvironment environment) => _environment = environment;

    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        if (options.AllowDevelopmentAnonymous && !_environment.IsDevelopment())
        {
            return ValidateOptionsResult.Fail(
                "Auth:AllowDevelopmentAnonymous is only permitted in Development.");
        }

        if (!_environment.IsProduction() || !options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.Authority))
        {
            return ValidateOptionsResult.Fail("Auth:Authority is required when Auth:Enabled is true in Production.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail("Auth:Audience is required when Auth:Enabled is true in Production.");
        }

        if (!options.RequireHttpsMetadata)
        {
            return ValidateOptionsResult.Fail("Auth:RequireHttpsMetadata must be true in Production.");
        }

        return ValidateOptionsResult.Success;
    }
}

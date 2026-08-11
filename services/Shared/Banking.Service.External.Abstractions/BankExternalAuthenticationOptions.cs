namespace Banking.Service.External.Abstractions;

public enum BankExternalAuthenticationMode
{
    None = 0,
    ApiKey = 1,
    OAuth2ClientCredentials = 2,
    MutualTls = 3
}

public sealed class BankExternalAuthenticationOptions
{
    public BankExternalAuthenticationMode Mode { get; set; } = BankExternalAuthenticationMode.None;

    public string? ApiKeyHeaderName { get; set; } = "X-Api-Key";

    public string? ApiKey { get; set; }

    public string? TokenEndpoint { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? Scope { get; set; }

    public string? ClientCertificatePath { get; set; }

    public string? ClientCertificatePassword { get; set; }
}

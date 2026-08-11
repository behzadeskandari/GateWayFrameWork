namespace Bank1.Service.Application.Configuration;

public sealed class Bank1ProxyOptions
{
    public const string SectionName = "Bank1Proxy";

    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "https://api.bank1.example/";

    public int TimeoutSeconds { get; set; } = 30;

    public Bank1ExternalEndpoints Endpoints { get; set; } = new();

    public Bank1ExternalResilienceOptions Resilience { get; set; } = new();

    public Bank1ExternalAuthenticationOptions Authentication { get; set; } = new();
}

public sealed class Bank1ExternalEndpoints
{
    public string GetAccountTemplate { get; set; } = "accounts/{accountId}";

    public string GetBalanceTemplate { get; set; } = "accounts/{accountId}/balance";
}

public sealed class Bank1ExternalResilienceOptions
{
    public int TimeoutSeconds { get; set; } = 30;

    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;
}

public sealed class Bank1ExternalAuthenticationOptions
{
    public string Mode { get; set; } = "None";

    public string? ApiKeyHeaderName { get; set; } = "X-Api-Key";

    public string? ApiKey { get; set; }

    public string? TokenEndpoint { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? Scope { get; set; }

    public string? ClientCertificatePath { get; set; }

    public string? ClientCertificatePassword { get; set; }
}

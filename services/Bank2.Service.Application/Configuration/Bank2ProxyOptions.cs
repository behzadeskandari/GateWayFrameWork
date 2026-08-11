namespace Bank2.Service.Application.Configuration;

public sealed class Bank2ProxyOptions
{
    public const string SectionName = "Bank2Proxy";

    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "https://api.bank2.example/";

    public int TimeoutSeconds { get; set; } = 30;

    public Bank2ExternalEndpoints Endpoints { get; set; } = new();

    public Bank2ExternalResilienceOptions Resilience { get; set; } = new();

    public Bank2ExternalAuthenticationOptions Authentication { get; set; } = new();
}

public sealed class Bank2ExternalEndpoints
{
    public string SubmitPaymentTemplate { get; set; } = "payments";

    public string SubmitTransferTemplate { get; set; } = "transfers";

    public string GetPaymentStatusTemplate { get; set; } = "payments/{paymentId}";

    public string GetTransferStatusTemplate { get; set; } = "transfers/{transferId}";
}

public sealed class Bank2ExternalResilienceOptions
{
    public int TimeoutSeconds { get; set; } = 30;

    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;
}

public sealed class Bank2ExternalAuthenticationOptions
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

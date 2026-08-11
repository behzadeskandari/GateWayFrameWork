namespace Gateway.Framework.Resilience;

public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public int MaxRetryAttempts { get; set; } = 2;
    public int RetryDelayMilliseconds { get; set; } = 200;
    public int CircuitBreakerSamplingDurationSeconds { get; set; } = 30;
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 15;
    public int TotalRequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Retry attempts for financial/payment clusters. Defaults to zero to avoid duplicate transactions.
    /// </summary>
    public int FinancialMaxRetryAttempts { get; set; } = 0;

    /// <summary>
    /// YARP cluster IDs that must not retry non-idempotent operations.
    /// </summary>
    public string[] FinancialClusterIds { get; set; } = ["payments-cluster"];

    public bool DisableRetryOnUnsafeHttpMethods { get; set; } = true;
}
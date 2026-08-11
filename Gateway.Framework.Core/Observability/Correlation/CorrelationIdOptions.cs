namespace Gateway.Framework.Core.Observability.Correlation;

public sealed class CorrelationIdOptions
{
    public const string SectionName = "CorrelationId";

    public string HeaderName { get; set; } = "X-Correlation-Id";
    public bool IncludeInResponse { get; set; } = true;
}

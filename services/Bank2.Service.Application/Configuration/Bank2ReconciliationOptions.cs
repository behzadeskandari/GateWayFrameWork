namespace Bank2.Service.Application.Configuration;

public sealed class Bank2ReconciliationOptions
{
    public const string SectionName = "Bank2Reconciliation";

    public bool Enabled { get; set; } = true;

    public int IntervalSeconds { get; set; } = 60;

    public int BatchSize { get; set; } = 50;

    public int MinimumAgeSeconds { get; set; } = 30;
}

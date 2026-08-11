namespace Bank2.Service.Application.Configuration;

public sealed class AuditDatabaseOptions
{
    public const string SectionName = "AuditDatabase";

    public string Provider { get; set; } = "Sqlite";
    public string ConnectionString { get; set; } = "Data Source=bank2-audit.db";
}

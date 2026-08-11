using Bank2.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bank2.Service.Tests;

public sealed class Bank2WebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseId = Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:ConnectionString", $"Data Source=bank2-test-{_databaseId}.db");
        builder.UseSetting("AuditDatabase:ConnectionString", $"Data Source=bank2-audit-test-{_databaseId}.db");
    }
}

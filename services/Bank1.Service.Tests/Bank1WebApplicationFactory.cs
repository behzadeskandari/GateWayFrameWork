using Bank1.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bank1.Service.Tests;

public sealed class Bank1WebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseId = Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:ConnectionString", $"Data Source=bank1-test-{_databaseId}.db");
        builder.UseSetting("AuditDatabase:ConnectionString", $"Data Source=bank1-audit-test-{_databaseId}.db");
    }
}

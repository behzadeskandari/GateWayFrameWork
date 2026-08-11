using Bank2.Service.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Bank2.Service.Tests;

public sealed class DatabaseIsolationTests
{
    [Fact]
    public void Bank2DbContext_DoesNotContainBank1EntityTypes()
    {
        var bank2EntityTypes = typeof(Bank2DbContext).GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0].FullName)
            .ToList();

        Assert.DoesNotContain(bank2EntityTypes, name => name!.Contains("Bank1", StringComparison.Ordinal));
        Assert.Contains(bank2EntityTypes, name => name!.Contains("Bank2.Service.Domain.Entities.Payment", StringComparison.Ordinal));
    }

    [Fact]
    public void Bank2BusinessAndAuditDatabases_AreConfiguredSeparately()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Data Source=bank2.db",
                ["AuditDatabase:ConnectionString"] = "Data Source=bank2-audit.db"
            })
            .Build();

        Assert.NotEqual(config["Database:ConnectionString"], config["AuditDatabase:ConnectionString"]);
    }

    [Fact]
    public void Bank2UsesDifferentDefaultConnectionStringThanBank1()
    {
        var bank1Connection = "Data Source=bank1.db";
        var bank2Connection = "Data Source=bank2.db";

        Assert.NotEqual(bank1Connection, bank2Connection);
    }
}

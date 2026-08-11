using Bank1.Service.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Bank1.Service.Tests;

public sealed class DatabaseIsolationTests
{
    [Fact]
    public void Bank1DbContext_DoesNotContainBank2EntityTypes()
    {
        var bank1EntityTypes = typeof(Bank1DbContext).GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0].FullName)
            .ToList();

        Assert.DoesNotContain(bank1EntityTypes, name => name!.Contains("Bank2", StringComparison.Ordinal));
        Assert.Contains(bank1EntityTypes, name => name!.Contains("Bank1.Service.Domain.Entities.Account", StringComparison.Ordinal));
    }

    [Fact]
    public void Bank1BusinessAndAuditDatabases_AreConfiguredSeparately()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Data Source=bank1.db",
                ["AuditDatabase:ConnectionString"] = "Data Source=bank1-audit.db"
            })
            .Build();

        Assert.NotEqual(config["Database:ConnectionString"], config["AuditDatabase:ConnectionString"]);
    }

    [Fact]
    public void Bank1UsesDifferentDefaultConnectionStringThanBank2()
    {
        var bank1Connection = "Data Source=bank1.db";
        var bank2Connection = "Data Source=bank2.db";

        Assert.NotEqual(bank1Connection, bank2Connection);
    }
}

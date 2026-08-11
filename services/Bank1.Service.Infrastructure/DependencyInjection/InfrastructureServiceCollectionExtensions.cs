using Banking.Service.Audit.Abstractions;
using Bank1.Service.Application.Abstractions.External;
using Bank1.Service.Application.Abstractions.Persistence;
using Bank1.Service.Application.Configuration;
using Bank1.Service.Infrastructure.ExternalServices;
using Bank1.Service.Infrastructure.HealthChecks;
using Bank1.Service.Infrastructure.Persistence;
using Bank1.Service.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bank1.Service.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddBank1Infrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var auditDatabaseOptions = configuration.GetSection(AuditDatabaseOptions.SectionName).Get<AuditDatabaseOptions>() ?? new AuditDatabaseOptions();
        var proxyOptions = configuration.GetSection(Bank1ProxyOptions.SectionName).Get<Bank1ProxyOptions>() ?? new Bank1ProxyOptions();

        services.AddDbContext<Bank1DbContext>(options =>
            ConfigureProvider(options, databaseOptions.Provider, databaseOptions.ConnectionString));

        services.AddDbContext<Bank1AuditDbContext>(options =>
            ConfigureProvider(
                options,
                auditDatabaseOptions.Provider,
                auditDatabaseOptions.ConnectionString,
                "__Bank1AuditMigrationsHistory"));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<DatabaseInitializer>();

        if (proxyOptions.Enabled)
        {
            services.AddHttpClient<IBank1Client, Bank1BankingProxy>(client =>
                {
                    client.BaseAddress = new Uri(proxyOptions.BaseUrl);
                })
                .AddBank1ProxyResilience();
        }
        else
        {
            services.AddScoped<IBank1Client, Bank1BankingProxy>();
        }

        services.AddHealthChecks()
            .AddCheck<Bank1DatabaseHealthCheck>("bank1-database", tags: ["ready"])
            .AddCheck<Bank1AuditDatabaseHealthCheck>("bank1-audit-database", tags: ["ready"]);

        return services;
    }

    private static void ConfigureProvider(
        DbContextOptionsBuilder options,
        string provider,
        string connectionString,
        string? migrationsHistoryTable = null)
    {
        if (string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                if (!string.IsNullOrWhiteSpace(migrationsHistoryTable))
                {
                    npgsql.MigrationsHistoryTable(migrationsHistoryTable);
                }
            });
            return;
        }

        options.UseSqlite(connectionString, sqlite =>
        {
            if (!string.IsNullOrWhiteSpace(migrationsHistoryTable))
            {
                sqlite.MigrationsHistoryTable(migrationsHistoryTable);
            }
        });
    }
}

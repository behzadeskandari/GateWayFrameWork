using Banking.Service.Audit.Abstractions;
using Bank2.Service.Application.Abstractions;
using Bank2.Service.Application.Abstractions.External;
using Bank2.Service.Application.Abstractions.Persistence;
using Bank2.Service.Application.Configuration;
using Bank2.Service.Infrastructure.ExternalServices;
using Bank2.Service.Infrastructure.HealthChecks;
using Bank2.Service.Infrastructure.Persistence;
using Bank2.Service.Infrastructure.Persistence.Repositories;
using Bank2.Service.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bank2.Service.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddBank2Infrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var auditDatabaseOptions = configuration.GetSection(AuditDatabaseOptions.SectionName).Get<AuditDatabaseOptions>() ?? new AuditDatabaseOptions();
        var proxyOptions = configuration.GetSection(Bank2ProxyOptions.SectionName).Get<Bank2ProxyOptions>() ?? new Bank2ProxyOptions();

        services.AddOptions<Bank2ProxyOptions>()
            .Bind(configuration.GetSection(Bank2ProxyOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<Bank2ProxyOptions>, Bank2ProxyOptionsValidator>();

        services.AddDbContext<Bank2DbContext>(options =>
            ConfigureProvider(options, databaseOptions.Provider, databaseOptions.ConnectionString));

        services.AddDbContext<Bank2AuditDbContext>(options =>
            ConfigureProvider(
                options,
                auditDatabaseOptions.Provider,
                auditDatabaseOptions.ConnectionString,
                "__Bank2AuditMigrationsHistory"));

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<IReconciliationService, Bank2ReconciliationService>();

        if (proxyOptions.Enabled)
        {
            services.AddBank2ExternalProxy(proxyOptions);
            services.AddScoped<IBank2ExternalGateway, Bank2ExternalGateway>();
        }

        services.AddHealthChecks()
            .AddCheck<Bank2DatabaseHealthCheck>("bank2-database", tags: ["ready"])
            .AddCheck<Bank2AuditDatabaseHealthCheck>("bank2-audit-database", tags: ["ready"]);

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

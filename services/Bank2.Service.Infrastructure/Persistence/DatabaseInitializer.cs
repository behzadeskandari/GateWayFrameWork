using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bank2.Service.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IServiceProvider serviceProvider, ILogger<DatabaseInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var bank2DbContext = scope.ServiceProvider.GetRequiredService<Bank2DbContext>();
        var auditDbContext = scope.ServiceProvider.GetRequiredService<Bank2AuditDbContext>();

        _logger.LogInformation("Applying Bank2 database migrations.");
        await bank2DbContext.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Applying Bank2 audit database migrations.");
        await auditDbContext.Database.MigrateAsync(cancellationToken);

        await DataSeeder.SeedAsync(bank2DbContext, cancellationToken);
        _logger.LogInformation("Bank2 database seed completed.");
    }
}

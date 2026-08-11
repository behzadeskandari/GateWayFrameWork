using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bank1.Service.Infrastructure.Persistence;

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
        var bank1DbContext = scope.ServiceProvider.GetRequiredService<Bank1DbContext>();
        var auditDbContext = scope.ServiceProvider.GetRequiredService<Bank1AuditDbContext>();

        _logger.LogInformation("Applying Bank1 database migrations.");
        await bank1DbContext.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Applying Bank1 audit database migrations.");
        await auditDbContext.Database.MigrateAsync(cancellationToken);

        await DataSeeder.SeedAsync(bank1DbContext, cancellationToken);
        _logger.LogInformation("Bank1 database seed completed.");
    }
}

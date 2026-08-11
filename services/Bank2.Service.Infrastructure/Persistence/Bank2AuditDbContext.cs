using Bank2.Service.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bank2.Service.Infrastructure.Persistence;

public sealed class Bank2AuditDbContext : DbContext
{
    public Bank2AuditDbContext(DbContextOptions<Bank2AuditDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Bank2AuditDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

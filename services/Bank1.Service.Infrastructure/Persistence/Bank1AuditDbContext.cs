using Bank1.Service.Infrastructure.Persistence.Configurations;
using Bank1.Service.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bank1.Service.Infrastructure.Persistence;

public sealed class Bank1AuditDbContext : DbContext
{
    public Bank1AuditDbContext(DbContextOptions<Bank1AuditDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditRecordConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

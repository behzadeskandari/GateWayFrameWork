using Bank2.Service.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bank2.Service.Infrastructure.Persistence;

public sealed class Bank2DbContext : DbContext
{
    public Bank2DbContext(DbContextOptions<Bank2DbContext> options)
        : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Bank2DbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

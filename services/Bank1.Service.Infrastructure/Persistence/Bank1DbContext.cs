using Bank1.Service.Domain.Entities;
using Bank1.Service.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Bank1.Service.Infrastructure.Persistence;

public sealed class Bank1DbContext : DbContext
{
    public Bank1DbContext(DbContextOptions<Bank1DbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

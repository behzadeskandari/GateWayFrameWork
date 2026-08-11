using Bank1.Service.Domain.Entities;
using Bank1.Service.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank1.Service.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasColumnName("id");

        builder.Property(account => account.HolderName)
            .HasColumnName("holder_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(account => account.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(account => account.Balance)
            .HasColumnName("balance")
            .HasPrecision(18, 2);

        builder.Property(account => account.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(account => account.OpenedAt)
            .HasColumnName("opened_at");

        builder.Property(account => account.RowVersion)
            .HasColumnName("row_version")
            .IsConcurrencyToken()
            .HasDefaultValue(new byte[] { 0 });

        builder.Property(account => account.AccountNumber)
            .HasColumnName("account_number")
            .HasMaxLength(64)
            .HasConversion(
                accountNumber => accountNumber.Value,
                value => new AccountNumber(value))
            .IsRequired();

        builder.HasIndex(account => account.AccountNumber)
            .HasDatabaseName("ix_accounts_account_number")
            .IsUnique();
    }
}

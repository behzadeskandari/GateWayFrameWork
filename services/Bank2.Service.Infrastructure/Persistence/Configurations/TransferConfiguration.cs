using Bank2.Service.Domain.Entities;
using Bank2.Service.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank2.Service.Infrastructure.Persistence.Configurations;

public sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers");

        builder.HasKey(transfer => transfer.Id);

        builder.Property(transfer => transfer.Id)
            .HasColumnName("id")
            .HasMaxLength(32);

        builder.Property(transfer => transfer.FromAccountId)
            .HasColumnName("from_account_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(transfer => transfer.ToAccountId)
            .HasColumnName("to_account_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(transfer => transfer.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2);

        builder.Property(transfer => transfer.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(transfer => transfer.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(transfer => transfer.Reference)
            .HasColumnName("reference")
            .HasMaxLength(256);

        builder.Property(transfer => transfer.BankReferenceId)
            .HasColumnName("bank_reference_id")
            .HasMaxLength(128);

        builder.Property(transfer => transfer.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(128);

        builder.Property(transfer => transfer.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(64);

        builder.Property(transfer => transfer.ErrorCode)
            .HasColumnName("error_code")
            .HasMaxLength(64);

        builder.Property(transfer => transfer.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(transfer => transfer.RowVersion)
            .HasColumnName("row_version")
            .IsConcurrencyToken()
            .HasDefaultValue(new byte[] { 0 });

        builder.HasIndex(transfer => transfer.FromAccountId)
            .HasDatabaseName("ix_transfers_from_account_id");

        builder.HasIndex(transfer => transfer.CreatedAt)
            .HasDatabaseName("ix_transfers_created_at");

        builder.HasIndex(transfer => transfer.Status)
            .HasDatabaseName("ix_transfers_status");

        builder.HasIndex(transfer => transfer.BankReferenceId)
            .HasDatabaseName("ix_transfers_bank_reference_id");
    }
}

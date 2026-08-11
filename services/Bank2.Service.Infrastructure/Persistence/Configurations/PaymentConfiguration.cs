using Bank2.Service.Domain.Entities;
using Bank2.Service.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank2.Service.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Id)
            .HasColumnName("id")
            .HasMaxLength(32);

        builder.Property(payment => payment.FromAccountId)
            .HasColumnName("from_account_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(payment => payment.ToAccountId)
            .HasColumnName("to_account_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(payment => payment.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2);

        builder.Property(payment => payment.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(payment => payment.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(payment => payment.Reference)
            .HasColumnName("reference")
            .HasMaxLength(256);

        builder.Property(payment => payment.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(payment => payment.RowVersion)
            .HasColumnName("row_version")
            .IsConcurrencyToken()
            .HasDefaultValue(new byte[] { 0 });

        builder.HasIndex(payment => payment.FromAccountId)
            .HasDatabaseName("ix_payments_from_account_id");

        builder.HasIndex(payment => payment.CreatedAt)
            .HasDatabaseName("ix_payments_created_at");
    }
}

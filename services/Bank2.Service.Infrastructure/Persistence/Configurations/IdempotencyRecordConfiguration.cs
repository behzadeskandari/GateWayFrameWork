using Bank2.Service.Domain.Entities;
using Bank2.Service.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank2.Service.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        builder.HasKey(record => record.Key);

        builder.Property(record => record.Key)
            .HasColumnName("key")
            .HasMaxLength(128);

        builder.Property(record => record.OperationType)
            .HasColumnName("operation_type")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(record => record.RequestFingerprint)
            .HasColumnName("request_fingerprint")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(record => record.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(record => record.ResponsePayload)
            .HasColumnName("response_payload");

        builder.Property(record => record.CreatedAt)
            .HasColumnName("created_at");

        builder.HasIndex(record => record.CreatedAt)
            .HasDatabaseName("ix_idempotency_records_created_at");
    }
}

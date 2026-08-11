using Bank1.Service.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank1.Service.Infrastructure.Persistence.Configurations;

public sealed class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        builder.ToTable("audit_records");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .HasColumnName("id");

        builder.Property(record => record.Timestamp)
            .HasColumnName("timestamp");

        builder.Property(record => record.ServiceName)
            .HasColumnName("service_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(record => record.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(record => record.Operation)
            .HasColumnName("operation")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(record => record.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(128);

        builder.Property(record => record.TraceId)
            .HasColumnName("trace_id")
            .HasMaxLength(128);

        builder.Property(record => record.RequestId)
            .HasColumnName("request_id")
            .HasMaxLength(128);

        builder.Property(record => record.ResourceType)
            .HasColumnName("resource_type")
            .HasMaxLength(64);

        builder.Property(record => record.ResourceId)
            .HasColumnName("resource_id")
            .HasMaxLength(128);

        builder.Property(record => record.ActorSubject)
            .HasColumnName("actor_subject")
            .HasMaxLength(256);

        builder.Property(record => record.Success)
            .HasColumnName("success");

        builder.Property(record => record.ErrorCode)
            .HasColumnName("error_code")
            .HasMaxLength(64);

        builder.Property(record => record.MetadataJson)
            .HasColumnName("metadata_json");

        builder.HasIndex(record => record.Timestamp)
            .HasDatabaseName("ix_audit_records_timestamp");

        builder.HasIndex(record => record.CorrelationId)
            .HasDatabaseName("ix_audit_records_correlation_id");
    }
}

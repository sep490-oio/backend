using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.PaymentContext.Aggregates.Webhooks;

namespace OIO.Infrastructure.Persistence.Configurations.PaymentContext;

internal sealed class GatewayWebhookEventConfiguration : IEntityTypeConfiguration<GatewayWebhookEvent>
{
    public void Configure(EntityTypeBuilder<GatewayWebhookEvent> builder)
    {
        builder.ToTable("gateway_webhook_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.RawContent)
            .HasColumnName("raw_content")
            .HasColumnType("jsonb") // Dùng jsonb cho PostgreSQL để lưu raw payload tối ưu
            .IsRequired();

        builder.ComplexProperty(e => e.ProcessingStatus, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("processing_status")
                .HasMaxLength(50)
                .IsRequired();
        }).HasComplexCompositeIndex(e => new { e.Provider, e.ProcessingStatus.Id }, indexName: "idx_gateway_webhooks_processing");

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(e => e.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.ProcessedAt)
            .HasColumnName("processed_at");

    }
}

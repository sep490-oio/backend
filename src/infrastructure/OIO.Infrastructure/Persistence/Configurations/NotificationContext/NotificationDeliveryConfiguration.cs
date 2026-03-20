using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.NotificationContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.NotificationContext;

internal sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_delivery");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.NotificationId)
            .HasColumnName("notification_id")
            .IsRequired();

        builder.Property(d => d.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.ComplexProperty(d => d.Channel, channelBuilder =>
        {
            channelBuilder.Property(c => c.Id)
                .HasColumnName("channel")
                .HasMaxLength(20)
                .IsRequired();
        }).HasComplexCompositeIndex(
            d => new { d.UserId, d.Channel.Id },
            indexName: "idx_delivery_user_channel");

        builder.ComplexProperty(d => d.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
        }).HasComplexCompositeIndex(
            d => new { d.Status.Id, d.ScheduledAt },
            indexName: "idx_delivery_status_scheduled",
            filter: "status = 'Pending'");

        builder.Property(d => d.AttemptCount)
            .HasColumnName("attempt_count")
            .HasDefaultValue(0);

        builder.Property(d => d.MaxAttempts)
            .HasColumnName("max_attempts")
            .HasDefaultValue(3);

        builder.Property(d => d.NextRetryAt)
            .HasColumnName("next_retry_at");

        builder.Property(d => d.DeliveryMetadata)
            .HasColumnName("delivery_metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");

        builder.Property(d => d.ErrorCode)
            .HasColumnName("error_code")
            .HasMaxLength(50);

        builder.Property(d => d.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(d => d.ErrorDetails)
            .HasColumnName("error_details")
            .HasColumnType("jsonb");

        builder.Property(d => d.ScheduledAt)
            .HasColumnName("scheduled_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(d => d.SentAt)
            .HasColumnName("sent_at");

        builder.Property(d => d.DeliveredAt)
            .HasColumnName("delivered_at");

        builder.Property(d => d.FailedAt)
            .HasColumnName("failed_at");

        // Indexes
        builder.HasIndex(d => d.NotificationId)
            .HasDatabaseName("idx_delivery_notification");

        builder.HasIndex(d => d.NextRetryAt)
            .HasDatabaseName("idx_delivery_retry")
            .HasFilter("status = 'Failed' AND next_retry_at IS NOT NULL");
    }
}

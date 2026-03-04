using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.NotificationContext.Aggregates.Notifications;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.NotificationContext.ValueObjects;
using OIO.Domain.Context.NotificationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Domain.Infrastructure.Persistence.Configurations.NotificationContext;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        // ==================== Primary Key ====================
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => NotificationId.From(value));

        // ==================== Properties ====================
        builder.Property(n => n.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(n => n.NotificationType)
            .HasColumnName("notification_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasColumnName("message")
            .IsRequired();

        // ==================== Metadata (jsonb → ComplexProperty) ====================
        builder.ComplexProperty(n => n.Metadata, metadataBuilder =>
        {
            metadataBuilder.Property(m => m.RawJson)
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .IsRequired();
        });

        // ==================== Entity Reference ====================
        builder.Property(n => n.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50);

        builder.Property(n => n.EntityId)
            .HasColumnName("entity_id");

        // ==================== RelatedEntities (jsonb → ComplexProperty) ====================
        builder.ComplexProperty(n => n.RelatedEntities, relatedBuilder =>
        {
            relatedBuilder.Property(r => r.RawJson)
                .HasColumnName("related_entities")
                .HasColumnType("jsonb")
                .IsRequired();
        });

        // ==================== Status & Priority ====================
        builder.Property(n => n.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(s => s.Id, value => NotificationStatus.FromId(value).GetValueOrThrow());

        builder.Property(n => n.Priority)
            .HasColumnName("priority")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(p => p.Id, value => NotificationPriority.FromId(value).GetValueOrThrow());

        // ==================== Actions (jsonb → ComplexProperty) ====================
        builder.ComplexProperty(n => n.Actions, actionsBuilder =>
        {
            actionsBuilder.Property(a => a.RawJson)
                .HasColumnName("actions")
                .HasColumnType("jsonb")
                .IsRequired();

            // Items is a parsed in-memory collection derived from RawJson — not persisted
            actionsBuilder.Ignore(a => a.Items);
        });

        // ==================== Timestamps ====================
        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(n => n.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(n => n.ReadAt)
            .HasColumnName("read_at");

        builder.Property(n => n.ExpiresAt)
            .HasColumnName("expires_at");

        // ==================== Relationships ====================
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_notifications_users_user_id");

        builder.HasMany(n => n.Deliveries)
            .WithOne()
            .HasForeignKey(d => d.NotificationId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_notification_delivery_notifications_notification_id");

        // ==================== Indexes ====================
        builder.HasIndex(n => new { n.UserId, n.CreatedAt })
            .HasDatabaseName("idx_notifications_user_created");

        builder.HasIndex(n => new { n.UserId, StatusId = n.Status })
            .HasDatabaseName("idx_notifications_user_status");

        builder.HasIndex(n => new { n.NotificationType, n.EventType })
            .HasDatabaseName("idx_notifications_type_event");

        builder.HasIndex(n => new { n.Priority, n.CreatedAt })
            .HasDatabaseName("idx_notifications_priority_created");

        builder.HasIndex(n => new { n.EntityType, n.EntityId })
            .HasDatabaseName("idx_notifications_entity");

        // ==================== Ignore ====================
        builder.Ignore(n => n.DomainEvents);
    }
}

internal sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_delivery");

        // ==================== Primary Key ====================
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => NotificationDeliveryId.From(value));

        // ==================== Properties ====================
        builder.Property(d => d.NotificationId)
            .HasColumnName("notification_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => NotificationId.From(value));

        builder.Property(d => d.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(d => d.Channel)
            .HasColumnName("channel")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(c => c.Id, value => DeliveryChannel.FromId(value).GetValueOrThrow());

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(s => s.Id, value => DeliveryStatus.FromId(value).GetValueOrThrow());

        builder.Property(d => d.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();

        builder.Property(d => d.MaxAttempts)
            .HasColumnName("max_attempts")
            .IsRequired();

        builder.Property(d => d.NextRetryAt)
            .HasColumnName("next_retry_at");

        // ==================== DeliveryMetadata (jsonb → ComplexProperty) ====================
        builder.ComplexProperty(d => d.Metadata, metadataBuilder =>
        {
            metadataBuilder.Property(m => m.RawJson)
                .HasColumnName("delivery_metadata")
                .HasColumnType("jsonb")
                .IsRequired();
        });

        // ==================== DeliveryError (nullable ComplexProperty) ====================
        // Error is a nullable value object — EF ComplexProperty does not support nullable complex
        // types directly, so we map each column individually and reconstruct in the domain.
        // The columns are nullable at the DB level; the domain's Error property is null when all
        // three columns are null.
        builder.ComplexProperty(d => d.Error, errorBuilder =>
        {
            errorBuilder.Property(e => e.Code)
                .HasColumnName("error_code")
                .HasMaxLength(50);

            errorBuilder.Property(e => e.Message)
                .HasColumnName("error_message");

            errorBuilder.Property(e => e.DetailsJson)
                .HasColumnName("error_details")
                .HasColumnType("jsonb");
        });

        // ==================== Timestamps ====================
        builder.Property(d => d.ScheduledAt)
            .HasColumnName("scheduled_at")
            .IsRequired();

        builder.Property(d => d.SentAt)
            .HasColumnName("sent_at");

        builder.Property(d => d.DeliveredAt)
            .HasColumnName("delivered_at");

        builder.Property(d => d.FailedAt)
            .HasColumnName("failed_at");

        // ==================== Relationships ====================
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_notification_delivery_users_user_id");

        // ==================== Indexes ====================
        builder.HasIndex(d => d.NotificationId)
            .HasDatabaseName("idx_delivery_notification");

        builder.HasIndex(d => d.NextRetryAt)
            .HasDatabaseName("idx_delivery_retry");

        builder.HasIndex(d => new { d.Status, d.ScheduledAt })
            .HasDatabaseName("idx_delivery_status_scheduled");

        builder.HasIndex(d => new { d.UserId, d.Channel })
            .HasDatabaseName("idx_delivery_user_channel");

        // ==================== Ignore computed ====================
        builder.Ignore(d => d.CanRetry);
        builder.Ignore(d => d.IsExhausted);
    }
}
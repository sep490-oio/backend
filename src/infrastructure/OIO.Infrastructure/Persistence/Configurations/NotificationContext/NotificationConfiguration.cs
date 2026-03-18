using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.NotificationContext.Aggregates;
using DomainNotification = OIO.Domain.Context.NotificationContext.Aggregates.Notification;

namespace OIO.Infrastructure.Persistence.Configurations.NotificationContext;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<DomainNotification>
{
    public void Configure(EntityTypeBuilder<DomainNotification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId)
            .HasColumnName("user_id")
            .IsRequired();

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

        builder.Property(n => n.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");

        builder.Property(n => n.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50);

        builder.Property(n => n.EntityId)
            .HasColumnName("entity_id");

        builder.Property(n => n.RelatedEntities)
            .HasColumnName("related_entities")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.ComplexProperty(n => n.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.ComplexProperty(n => n.Priority, priorityBuilder =>
        {
            priorityBuilder.Property(p => p.Id)
                .HasColumnName("priority")
                .HasMaxLength(20)
                .IsRequired();
        }).HasComplexCompositeIndex(
            n => new { n.Priority.Id, n.CreatedAt },
            indexName: "idx_notifications_priority_created",
            filter: "status = 'unread'");

        builder.Property(n => n.Actions)
            .HasColumnName("actions")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(n => n.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(n => n.ReadAt)
            .HasColumnName("read_at");

        builder.Property(n => n.ExpiresAt)
            .HasColumnName("expires_at");

        // Navigation
        builder.HasMany(n => n.Deliveries)
            .WithOne(d => d.Notification)
            .HasForeignKey(d => d.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(n => new { n.UserId, n.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_notifications_user_created");

        builder.HasIndex(n => new { n.NotificationType, n.EventType })
            .HasDatabaseName("idx_notifications_type_event");

        builder.HasIndex(n => new { n.EntityType, n.EntityId })
            .HasDatabaseName("idx_notifications_entity")
            .HasFilter("entity_id IS NOT NULL");
    }
}

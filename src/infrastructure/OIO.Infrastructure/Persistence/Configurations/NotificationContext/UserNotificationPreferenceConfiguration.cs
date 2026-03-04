using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.NotificationContext.Aggregates.UserNotificationPreferences;
using OIO.Domain.Context.NotificationContext.ValueObjects;
using OIO.Domain.Context.NotificationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Domain.Infrastructure.Persistence.Configurations.NotificationContext;

internal sealed class UserNotificationPreferenceConfiguration
    : IEntityTypeConfiguration<UserNotificationPreference>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreference> builder)
    {
        builder.ToTable("user_notification_preferences");

        // ==================== Primary Key ====================
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => UserNotificationPreferenceId.From(value));

        // ==================== Properties ====================
        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(p => p.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        // ==================== TypePreferences (jsonb → ComplexProperty) ====================
        builder.ComplexProperty(p => p.TypePreferences, tpBuilder =>
        {
            tpBuilder.Property(t => t.RawJson)
                .HasColumnName("type_preferences")
                .HasColumnType("jsonb")
                .IsRequired();
        });

        // ==================== NotificationChannels (jsonb → ComplexProperty) ====================
        builder.ComplexProperty(p => p.Channels, channelsBuilder =>
        {
            channelsBuilder.Property(c => c.RawJson)
                .HasColumnName("channels")
                .HasColumnType("jsonb")
                .IsRequired();
        });

        // ==================== QuietHours (nullable jsonb → ComplexProperty) ====================
        builder.ComplexProperty(p => p.QuietHours, qhBuilder =>
        {
            qhBuilder.Property(q => q.RawJson)
                .HasColumnName("quiet_hours")
                .HasColumnType("jsonb");
        });

        // ==================== RateLimits (nullable jsonb → ComplexProperty) ====================
        builder.ComplexProperty(p => p.RateLimits, rlBuilder =>
        {
            rlBuilder.Property(r => r.RawJson)
                .HasColumnName("rate_limits")
                .HasColumnType("jsonb");
        });

        // ==================== Timestamps ====================
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.ModifiedAt)
            .HasColumnName("modified_at");

        // ==================== Relationships ====================
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_user_notification_preferences_users_user_id");

        // ==================== Indexes ====================
        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasDatabaseName("idx_preferences_user");

        // ==================== Ignore ====================
        builder.Ignore(p => p.DomainEvents);
    }
}
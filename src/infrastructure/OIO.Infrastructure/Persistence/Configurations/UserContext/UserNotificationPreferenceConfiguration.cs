using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class UserNotificationPreferenceConfiguration : IEntityTypeConfiguration<UserNotificationPreference>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreference> builder)
    {
        builder.ToTable("user_notification_preferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(p => p.IsEnabled)
            .HasColumnName("is_enabled")
            .HasDefaultValue(true);

        builder.Property(p => p.TypePreferences)
            .HasColumnName("type_preferences")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(p => p.Channels)
            .HasColumnName("channels")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{\"push\": true, \"email\": true, \"sms\": false}'::jsonb")
            .IsRequired();

        builder.Property(p => p.QuietHours)
            .HasColumnName("quiet_hours")
            .HasColumnType("jsonb");

        builder.Property(p => p.RateLimits)
            .HasColumnName("rate_limits")
            .HasColumnType("jsonb");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(p => p.ModifiedAt)
            .HasColumnName("modified_at");

        // Constraints
        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasDatabaseName("uq_user_notification_preferences_user");
    }
}

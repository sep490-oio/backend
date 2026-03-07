using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => UserSessionId.From(value));

        builder.Property(f => f.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(f => f.DeviceId)
            .HasColumnName("device_id")
            .IsRequired();

        builder.Property(f => f.UserAgent)
            .HasColumnName("user_agent")
            .IsRequired();

        builder.Property(f => f.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("inet")
            .IsRequired();

        builder.Property(f => f.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(f => f.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();
        
        builder.Property(f => f.AbsoluteExpiresAt)
            .HasColumnName("absolute_expires_at")
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(f => f.LastRotatedAt)
            .HasColumnName("last_rotated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(f => f.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(f => f.RevokedReason)
            .HasColumnName("revoked_reason");

        // ==================== Relationships ====================
        builder.HasMany(f => f.Tokens)
            .WithOne(t => t.RefreshTokenFamily)
            .HasForeignKey(t => t.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==================== Indexes ====================
        builder.HasIndex(f => f.UserId)
            .HasDatabaseName("ix_user_sessions_user");

        builder.HasIndex(f => new { f.UserId, f.IsActive })
            .HasDatabaseName("ix_user_sessions_user_active");
        
        builder.HasIndex(f => f.ExpiresAt)
            .HasDatabaseName("ix_user_sessions_expires_at");
        
        builder.HasIndex(f => f.AbsoluteExpiresAt)
            .HasDatabaseName("ix_user_sessions_absolute_expires_at");
        
        
    }
}
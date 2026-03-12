using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
{
    public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
    {
        builder.ToTable("user_refresh_tokens", t =>
        {
            t.HasCheckConstraint("chk_expires_after_created",
                "expires_at > created_at");

            t.HasCheckConstraint("chk_revoked_after_created",
                "revoked_at IS NULL OR revoked_at >= created_at");

            t.HasCheckConstraint("chk_used_after_created",
                "used_at IS NULL OR used_at >= created_at");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => UserRefreshTokenId.From(value));

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(t => t.TokenHash)
            .HasColumnName("token_hash")
            .IsRequired();

        builder.Property(t => t.SessionId)
            .HasColumnName("session_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserSessionId.From(value));

        builder.Property(t => t.ParentTokenId)
            .HasColumnName("parent_token_id")
            .HasConversion(x => x!.Value.Value, value => UserRefreshTokenId.From(value));

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(t => t.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(t => t.RevokedReason)
            .HasColumnName("revoked_reason");

        builder.Property(t => t.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasColumnType("inet")
            .IsRequired();

        builder.Property(t => t.RevokedByIp)
            .HasColumnName("revoked_by_ip")
            .HasColumnType("inet");

        builder.Property(t => t.RotationCounter)
            .HasColumnName("rotation_counter")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(t => t.IsUsed)
            .HasColumnName("is_used")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(t => t.UsedAt)
            .HasColumnName("used_at");

        // ==================== Self-referencing relationship ====================
        builder.HasOne<UserRefreshToken>()
            .WithMany()
            .HasForeignKey(t => t.ParentTokenId)
            .OnDelete(DeleteBehavior.SetNull);

        // ==================== Indexes ====================
        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("ix_user_refresh_tokens_token_hash");

        builder.HasIndex(t => t.SessionId)
            .HasDatabaseName("ix_user_refresh_session_id");

        builder.HasIndex(t => new { SessionId = t.SessionId, t.CreatedAt })
            .HasDatabaseName("ix_user_refresh_tokens_session_session_id_created_at");

        builder.HasIndex(t => t.ExpiresAt)
            .HasDatabaseName("ix_user_refresh_tokens_expires_at");

        builder.HasIndex(t => new { t.UserId, t.IsUsed })
            .HasDatabaseName("ix_user_refresh_tokens_user_id_is_used");

        // ==================== Ignore computed properties ====================
        builder.Ignore(t => t.IsRevoked);
        
        builder.HasQueryFilter(d => d.User.DeletedAt == null);
    }
}
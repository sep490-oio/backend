using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class RecoveryCodeConfiguration : IEntityTypeConfiguration<RecoveryCode>
{
    public void Configure(EntityTypeBuilder<RecoveryCode> builder)
    {
        builder.ToTable("recovery_codes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => RecoveryCodeId.From(value));

        builder.Property(r => r.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(r => r.CodeHash)
            .HasColumnName("code_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(r => r.IsUsed)
            .HasColumnName("is_used")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(r => r.UsedAt)
            .HasColumnName("used_at");

        // ==================== Indexes ====================
        builder.HasIndex(r => new { r.UserId, r.IsUsed })
            .HasDatabaseName("idx_recovery_codes_user_unused")
            .HasFilter("is_used = false");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class UserRiskFlagConfiguration : IEntityTypeConfiguration<UserRiskFlag>
{
    public void Configure(EntityTypeBuilder<UserRiskFlag> builder)
    {
        builder.ToTable("user_risk_flags");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(f => f.FlagType)
            .HasColumnName("flag_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.Reason)
            .HasColumnName("reason");

        builder.ComplexProperty(f => f.Severity, severityBuilder =>
        {
            severityBuilder.Property(s => s.Id)
                .HasColumnName("severity")
                .HasMaxLength(10)
                .IsRequired();
        });

        builder.Property(f => f.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Indexes
        builder.HasIndex(f => f.UserId)
            .HasDatabaseName("idx_user_risk_flags_user");
    }
}

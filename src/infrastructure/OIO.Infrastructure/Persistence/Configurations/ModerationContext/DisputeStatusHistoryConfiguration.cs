using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class DisputeStatusHistoryConfiguration : IEntityTypeConfiguration<DisputeStatusHistory>
{
    public void Configure(EntityTypeBuilder<DisputeStatusHistory> builder)
    {
        builder.ToTable("dispute_status_history");

        builder.HasKey(sh => sh.Id);

        builder.Property(sh => sh.DisputeId)
            .HasColumnName("dispute_id")
            .IsRequired();

        builder.Property(sh => sh.OldStatus)
            .HasColumnName("old_status")
            .HasMaxLength(30);

        builder.Property(sh => sh.NewStatus)
            .HasColumnName("new_status")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(sh => sh.ChangedBy)
            .HasColumnName("changed_by");

        builder.Property(sh => sh.Reason)
            .HasColumnName("reason");

        builder.Property(sh => sh.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}

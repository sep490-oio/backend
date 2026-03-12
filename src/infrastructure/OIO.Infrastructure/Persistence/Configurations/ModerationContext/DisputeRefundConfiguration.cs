using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class DisputeRefundConfiguration : IEntityTypeConfiguration<DisputeRefund>
{
    public void Configure(EntityTypeBuilder<DisputeRefund> builder)
    {
        builder.ToTable("dispute_refunds");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.DisputeId)
            .HasColumnName("dispute_id")
            .IsRequired();

        builder.Property(r => r.TransactionId)
            .HasColumnName("transaction_id")
            .IsRequired();

        builder.ComplexProperty(r => r.RefundType, refundTypeBuilder =>
        {
            refundTypeBuilder.Property(rt => rt.Id)
                .HasColumnName("refund_type")
                .HasMaxLength(30)
                .IsRequired();
        });

        builder.Property(r => r.Reason)
            .HasColumnName("reason")
            .IsRequired();

        builder.Property(r => r.ApprovedBy)
            .HasColumnName("approved_by");

        builder.Property(r => r.ApprovedAt)
            .HasColumnName("approved_at");

        builder.Property(r => r.Notes)
            .HasColumnName("notes");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Constraints
        builder.HasIndex(r => r.TransactionId)
            .IsUnique()
            .HasDatabaseName("uq_dispute_refunds_transaction_id");
    }
}

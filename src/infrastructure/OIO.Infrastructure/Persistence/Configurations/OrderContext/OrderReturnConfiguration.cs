using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;

namespace OIO.Infrastructure.Persistence.Configurations.OrderContext;

internal sealed class OrderReturnConfiguration : IEntityTypeConfiguration<OrderReturn>
{
    public void Configure(EntityTypeBuilder<OrderReturn> builder)
    {
        builder.ToTable("order_returns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(r => r.BuyerId)
            .HasColumnName("buyer_id")
            .IsRequired();

        builder.Property(r => r.ReasonCode)
            .HasColumnName("reason_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description");

        builder.ComplexProperty(r => r.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(30)
                .IsRequired();
        });

        builder.Property(r => r.RequestedAt)
            .HasColumnName("requested_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(r => r.ApprovedAt)
            .HasColumnName("approved_at");

        builder.Property(r => r.RejectedAt)
            .HasColumnName("rejected_at");

        builder.Property(r => r.SellerConfirmedReceivedAt)
            .HasColumnName("seller_confirmed_received_at");

        builder.Property(r => r.BuyerDecisionDueAt)
            .HasColumnName("buyer_decision_due_at");

        // Indexes
        builder.HasIndex(r => r.OrderId)
            .IsUnique()
            .HasDatabaseName("uq_order_returns_order");

        builder.HasIndex(r => r.BuyerId)
            .HasDatabaseName("idx_order_returns_buyer");
    }
}

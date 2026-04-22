using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.OrderContext;

internal sealed class OrderReturnEvidenceConfiguration
    : IEntityTypeConfiguration<OrderReturnEvidence>
{
    public void Configure(EntityTypeBuilder<OrderReturnEvidence> builder)
    {
        builder.ToTable("order_return_evidence");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, v => OrderReturnEvidenceId.From(v));

        builder.Property(e => e.OrderReturnId)
            .HasColumnName("order_return_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => OrderReturnId.From(v));

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.MediaUploadId)
            .HasColumnName("media_upload_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => MediaUploadId.From(v));

        builder.Property(e => e.SecureUrl)
            .HasColumnName("secure_url")
            .HasMaxLength(1000);

        builder.Property(e => e.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(500);

        builder.Property(e => e.ResourceType)
            .HasColumnName("resource_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired()
            .HasConversion(x => x.Value, v => UserId.From(v));

        // Indexes
        builder.HasIndex(e => e.OrderReturnId)
            .HasDatabaseName("idx_order_return_evidence_order_return_id");

        builder.HasIndex(e => new { e.OrderReturnId, e.Category })
            .HasDatabaseName("idx_order_return_evidence_return_category");
    }
}

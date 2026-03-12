using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class ReviewQueueConfiguration : IEntityTypeConfiguration<ReviewQueue>
{
    public void Configure(EntityTypeBuilder<ReviewQueue> builder)
    {
        builder.ToTable("review_queue");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(q => q.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        builder.Property(q => q.PriorityScore)
            .HasColumnName("priority_score")
            .HasColumnType("numeric(10,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(q => q.AssignedTo)
            .HasColumnName("assigned_to");

        builder.ComplexProperty(q => q.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();
        }).HasComplexCompositeIndex(
            q => new { q.Status.Id, q.PriorityScore, q.CreatedAt },
            indexName: "idx_review_queue_status_priority");

        builder.Property(q => q.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Indexes
        builder.HasIndex(q => new { q.EntityType, q.EntityId })
            .HasDatabaseName("idx_review_queue_entity");

        builder.HasIndex(q => q.AssignedTo)
            .HasDatabaseName("idx_review_queue_assigned_to");
    }
}

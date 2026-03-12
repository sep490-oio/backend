using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class AdminReviewTaskConfiguration : IEntityTypeConfiguration<AdminReviewTask>
{
    public void Configure(EntityTypeBuilder<AdminReviewTask> builder)
    {
        builder.ToTable("admin_review_tasks");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        builder.Property(a => a.AssignedTo)
            .HasColumnName("assigned_to");

        builder.ComplexProperty(a => a.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();
        }).HasComplexCompositeIndex(
            a => new { Status = a.Status.Id, Priority = a.Priority.Id },
            indexName: "idx_admin_review_tasks_status_priority");

        builder.ComplexProperty(a => a.Priority, priorityBuilder =>
        {
            priorityBuilder.Property(p => p.Id)
                .HasColumnName("priority")
                .HasMaxLength(10)
                .IsRequired();
        });

        builder.Property(a => a.DueAt)
            .HasColumnName("due_at");

        builder.Property(a => a.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(a => a.ModifiedAt)
            .HasColumnName("modified_at");

        // Indexes
        builder.HasIndex(a => new { a.EntityType, a.EntityId })
            .HasDatabaseName("idx_admin_review_tasks_entity");

        builder.HasIndex(a => a.AssignedTo)
            .HasDatabaseName("idx_admin_review_tasks_assigned_to");

        builder.HasIndex(a => a.DueAt)
            .HasDatabaseName("idx_admin_review_tasks_due_at")
            .HasFilter("due_at IS NOT NULL");
    }
}

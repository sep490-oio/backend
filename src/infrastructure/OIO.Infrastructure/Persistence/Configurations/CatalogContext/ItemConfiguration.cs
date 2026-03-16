using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.CatalogContext;

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items", t =>
        {
            t.HasCheckConstraint("chk_items_resubmission_count", "resubmission_count >= 0");
        });
        
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired()
            .HasConversion(
                x => x.Value,
                value => ItemId.From(value));
        
        builder.Property(i => i.SellerId)
            .HasColumnName("seller_id")
            .IsRequired()
            .HasConversion(
                x => x.Value,
                value => UserId.From(value));
        
        builder.Property(i => i.CategoryId)
            .HasColumnName("category_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : default(Guid?),
                value => value.HasValue ? CategoryId.From(value.Value) : null);

        builder.ComplexProperty(i => i.Title, titleBuilder =>
        {
            titleBuilder.Property(t => t.Value)
                .HasColumnName("title")
                .IsRequired();
        });

        builder.Property(i => i.Description)
            .HasColumnName("description");

        builder.ComplexProperty(i => i.Condition, conditionBuilder =>
        {
            conditionBuilder.Property(c => c.Id)
                .HasColumnName("condition")
                .IsRequired();
        });

        builder.ComplexProperty(i => i.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasDefaultValue(ItemStatus.Draft.Id)
                .IsRequired()
                .HasComplexIndex(indexName: "idx_items_status");
        }).HasComplexCompositeIndex(i => new { i.Status.Id, i.SubmittedAt }, indexName: "idx_items_status_submitted_at", filter: "status IN ('submitted', 'under_review')");

        builder.Property(i => i.Quantity)
            .HasColumnName("quantity")
            .IsRequired();
        
        builder.Property(i => i.Attributes)
            .HasColumnName("attributes")
            .HasColumnType("jsonb");

        builder.Property(i => i.SubmittedAt)
            .HasColumnName("submitted_at");
        
        builder.Property(i => i.ReviewedAt)
            .HasColumnName("reviewed_at");
        
        builder.Property(i => i.ReviewedBy)
            .HasColumnName("reviewed_by")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : default(Guid?),
                value => value.HasValue ? UserId.From(value.Value) : null);

        builder.Property(i => i.RejectionReason)
            .HasColumnName("rejection_reason");
        
        builder.Property(i => i.ResubmissionCount)
            .HasColumnName("resubmission_count")
            .IsRequired();
        
        builder.Property(i => i.AssignedAdminId)
            .HasColumnName("assigned_admin_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : default(Guid?),
                value => value.HasValue ? UserId.From(value.Value) : null);

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(i => i.ModifiedAt)
            .HasColumnName("modified_at");
        
        //Index
        builder.HasIndex(i => i.SellerId)
            .HasDatabaseName("idx_items_seller_id");
        
        builder.HasIndex(i => i.CategoryId)
            .HasDatabaseName("idx_items_category_id");
        
        builder.HasIndex(i => i.AssignedAdminId)
            .HasDatabaseName("idx_items_assigned_admin_id");
        
        builder.HasIndex(i => i.ReviewedBy)
            .HasDatabaseName("idx_items_reviewed_by");

        builder.HasIndex(i => i.SubmittedAt)
            .HasDatabaseName("idx_items_submitted_at");
        
        
        //Relationship
        builder.HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(i => i.Media)
            .WithOne(m => m.Item)
            .HasForeignKey(img => img.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Questions)
            .WithOne()
            .HasForeignKey(q => q.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        
        builder.HasMany(i => i.ModerationReviews)
            .WithOne(mr => mr.Item)
            .HasForeignKey(mr => mr.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        //Ignores
        builder.Ignore(i => i.IsAvailableForAuction);
    }
}
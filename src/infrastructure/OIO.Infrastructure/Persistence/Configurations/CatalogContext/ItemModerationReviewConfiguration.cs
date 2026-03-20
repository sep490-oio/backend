using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.CatalogContext;

internal sealed class ItemModerationReviewConfiguration : IEntityTypeConfiguration<ItemModerationReview>
{
    public void Configure(EntityTypeBuilder<ItemModerationReview> builder)
    {
        builder.ToTable("item_moderation_reviews");
        
        builder.HasKey(im => im.Id);
        
        builder.Property(im => im.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired()
            .HasConversion(
                x => x.Value,
                value => ItemModerationReviewId.From(value));
        
        builder.Property(im => im.ItemId)
            .HasColumnName("item_id")
            .IsRequired()
            .HasConversion(
                x => x.Value,
                value => ItemId.From(value));

        builder.ComplexProperty(im => im.Action, moderationActionBuilder =>
        {
            moderationActionBuilder.Property(a => a.Id)
                .HasColumnName("action")
                .IsRequired();
        });
        
        builder.Property(im => im.ReviewerId)
            .HasColumnName("reviewer_id")
            .IsRequired()
            .HasConversion(
                x => x.Value,
                value => UserId.From(value));
        
        builder.Property(im => im.Reason)
            .HasColumnName("reason");
        
        builder.Property(im => im.OldStatus)
            .HasColumnName("old_status");
        
        builder.Property(im => im.NewStatus)
            .HasColumnName("new_status");
        
        builder.Property(im => im.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        //Index
        builder.HasIndex(im => im.ItemId)
            .HasDatabaseName("idx_item_moderation_reviews_item_id");
        
        builder.HasIndex(im => im.ReviewerId)
            .HasDatabaseName("idx_item_moderation_reviews_reviewer_id");
        
        builder.HasIndex(im => im.CreatedAt)
            .HasDatabaseName("idx_item_moderation_reviews_created_at");
    }
}
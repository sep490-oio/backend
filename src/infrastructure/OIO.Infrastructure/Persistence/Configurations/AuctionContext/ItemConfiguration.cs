using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => ItemId.From(x));

        builder.Property(i => i.SellerId)
            .HasColumnName("seller_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => UserId.From(x));

        builder.Property(i => i.CategoryId)
            .HasColumnName("category_id")
            .HasConversion(x => x != null ? (Guid?)x.Value : null, x => x.HasValue ? CategoryId.From(x.Value) : null);

        builder.Property(i => i.Title)
            .HasColumnName("title")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(i => i.Description)
            .HasColumnName("description");

        builder.Property(i => i.Condition)
            .HasColumnName("condition")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion(x => x.Id, x => ItemCondition.FromId(x).GetValueOrThrow());

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(ItemStatus.Draft)
            .IsRequired()
            .HasConversion(x => x.Id, x => ItemStatus.FromId(x).GetValueOrThrow());

        builder.Property(i => i.Quantity)
            .HasColumnName("quantity")
            .HasDefaultValue(1);

        builder.Property(i => i.Attributes)
            .HasColumnName("attributes")
            .HasColumnType("jsonb");

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(i => i.ModifiedAt)
            .HasColumnName("modified_at");

        // ==================== Relationships ====================
        builder.HasMany(i => i.Media)
            .WithOne()
            .HasForeignKey(img => img.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Questions)
            .WithOne()
            .HasForeignKey(q => q.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==================== Indexes ====================
        builder.HasIndex(i => i.SellerId)
            .HasDatabaseName("idx_items_seller");

        builder.HasIndex(i => i.CategoryId)
            .HasDatabaseName("idx_items_category");

        // ==================== Ignore ====================
        builder.Ignore(i => i.DomainEvents);
        builder.Ignore(i => i.Images);
        builder.Ignore(i => i.Videos);
        builder.Ignore(i => i.Documents);
        builder.Ignore(i => i.PrimaryImage);
        
    }
}
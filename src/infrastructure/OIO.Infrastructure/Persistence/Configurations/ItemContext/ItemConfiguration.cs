using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OIO.Domain.Context.AuctionContext.Aggregates.Categories;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.ItemContext;

public sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                value => ItemId.From(value));

        builder.Property(i => i.SellerId)
            .HasColumnName("seller_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => UserId.From(value));

        builder.Property(i => i.CategoryId)
            .HasColumnName("category_id")
            .HasColumnType("uuid")
            .IsRequired(false)
            .HasConversion(
                new ValueConverter<CategoryId?, Guid?>(
                    id => id == null ? (Guid?)null : (Guid?)id.Value,
                    v  => v == null ? null : CategoryId.From(v.Value)));

        builder.Property(i => i.Title)
            .HasColumnName("title")
            .HasColumnType("character varying(255)")
            .IsRequired();

        builder.Property(i => i.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(i => i.Condition)
            .HasColumnName("condition")
            .HasColumnType("character varying(50)")
            .IsRequired()
            .HasConversion(
                c => c.ToString().ToLowerInvariant(),
                s => Enum.Parse<ItemCondition>(s, ignoreCase: true));

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasColumnType("character varying(20)")
            .IsRequired()
            .HasDefaultValue(ItemStatus.Draft)
            .HasConversion(
                s => s.ToString().ToLowerInvariant(),
                s => Enum.Parse<ItemStatus>(s, ignoreCase: true));

        builder.Property(i => i.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("integer")
            .HasDefaultValue(1);

        // jsonb — serialisation is the application layer's concern
        builder.Property(i => i.Attributes)
            .HasColumnName("attributes")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(i => i.ModifiedAt)
            .HasColumnName("modified_at")
            .HasColumnType("timestamp")
            .IsRequired(false);

        builder.HasIndex(i => i.SellerId)
            .HasDatabaseName("idx_items_seller");

        builder.HasIndex(i => i.CategoryId)
            .HasDatabaseName("idx_items_category");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "items_condition_check",
                "condition IN ('new','like_new','very_good','good','acceptable')");
            t.HasCheckConstraint(
                "items_status_check",
                "status IN ('draft','active','in_auction','sold','removed')");
        });

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("items_category_id_fkey");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.SellerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("items_seller_id_fkey");

        builder.OwnsMany(i => i.Images, img =>
        {
            img.ToTable("item_images");
            // CHANGE THIS LINE:
            img.WithOwner().HasForeignKey(x => x.ItemId); 

            img.HasKey(x => x.Id);
    img.Property(x => x.Id)
        .HasColumnName("id")
        .HasColumnType("uuid")
        .ValueGeneratedNever()
        .HasConversion(
            id => id.Value,
            value => ItemImageId.From(value));

    img.Property(x => x.ItemId)
        .HasColumnName("item_id")
        .HasColumnType("uuid")
        .IsRequired()
        .HasConversion(
            id => id.Value,
            value => ItemId.From(value));

    img.Property(x => x.ImageUrl)
        .HasColumnName("image_url")
        .HasColumnType("character varying(500)")
        .IsRequired();

    img.Property(x => x.IsPrimary)
        .HasColumnName("is_primary")
        .HasColumnType("boolean")
        .HasDefaultValue(false);

    img.Property(x => x.SortOrder)
        .HasColumnName("sort_order")
        .HasColumnType("integer")
        .HasDefaultValue(0);

    img.Property(x => x.CreatedAt)
        .HasColumnName("created_at")
        .HasColumnType("timestamp")
        .IsRequired()
        .HasDefaultValueSql("CURRENT_TIMESTAMP");
});

        builder.OwnsMany(i => i.Questions, q =>
        {
            q.ToTable("item_questions");
            // CHANGE THIS LINE:
            q.WithOwner().HasForeignKey(x => x.ItemId);

            q.HasKey(x => x.Id);
    q.Property(x => x.Id)
        .HasColumnName("id")
        .HasColumnType("uuid")
        .ValueGeneratedNever()
        .HasConversion(
            id => id.Value,
            value => ItemQuestionId.From(value));

    q.Property(x => x.ItemId)
        .HasColumnName("item_id")
        .HasColumnType("uuid")
        .IsRequired()
        .HasConversion(
            id => id.Value,
            value => ItemId.From(value));

    q.Property(x => x.AskerId)
        .HasColumnName("asker_id")
        .HasColumnType("uuid")
        .IsRequired()
        .HasConversion(
            id => id.Value,
            value => UserId.From(value));

    q.Property(x => x.Question)
        .HasColumnName("question")
        .HasColumnType("text")
        .IsRequired();

    q.Property(x => x.Answer)
        .HasColumnName("answer")
        .HasColumnType("text")
        .IsRequired(false);

    q.Property(x => x.AnsweredAt)
        .HasColumnName("answered_at")
        .HasColumnType("timestamp")
        .IsRequired(false);

    q.Property(x => x.IsPublic)
        .HasColumnName("is_public")
        .HasColumnType("boolean")
        .HasDefaultValue(true);

    q.Property(x => x.CreatedAt)
        .HasColumnName("created_at")
        .HasColumnType("timestamp")
        .IsRequired()
        .HasDefaultValueSql("CURRENT_TIMESTAMP");
});

        builder.Ignore(i => i.DomainEvents);
    }
}
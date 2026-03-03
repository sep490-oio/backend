using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OIO.Domain.Context.AuctionContext.Aggregates.Categories;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.CategoryContext;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // ── Table ────────────────────────────────────────────────────────────
        builder.ToTable("categories");

        // ── Primary Key ──────────────────────────────────────────────────────
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                value => CategoryId.From(value));

        // ── Scalar Properties ────────────────────────────────────────────────
        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasColumnType("character varying(100)")
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasColumnName("slug")
            .HasColumnType("character varying(100)")
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .HasDefaultValue(true);

        // ── Timestamps ───────────────────────────────────────────────────────
        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // ModifiedAt is on IAuditableEntity but has no column in the schema — ignore it.
        builder.Ignore(c => c.ModifiedAt);

        // ── Shadow Properties (DB columns with no domain counterpart) ────────
        builder.Property(c => c.ParentId)
            .HasColumnName("parent_id")
            .HasColumnType("uuid")
            .IsRequired(false)
            .HasConversion(
                new ValueConverter<CategoryId?, Guid?>(
                    id => id == null ? (Guid?)null : (Guid?)id.Value,
                    v  => v == null ? null : CategoryId.From(v.Value)));

        builder.Property(c => c.IconUrl)
            .HasColumnName("icon_url")
            .HasColumnType("character varying(500)")
            .IsRequired(false);

        builder.Property(c => c.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("integer")
            .HasDefaultValue(0);

        builder.Property(c => c.Path)
            .HasColumnName("path")
            .HasColumnType("text")
            .IsRequired(false);

        // ── Indexes ──────────────────────────────────────────────────────────
        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasDatabaseName("categories_slug_key");

        // ── Self-referencing relationship (via shadow property) ───────────────
        // ── Self-referencing relationship ───────────────
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentId) // Tell EF to use your existing property!
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("categories_parent_id_fkey");

        // ── Ignore domain-event collection ───────────────────────────────────
        builder.Ignore(c => c.DomainEvents);
    }
}
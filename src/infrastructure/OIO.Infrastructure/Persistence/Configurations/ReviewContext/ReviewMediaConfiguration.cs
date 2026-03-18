using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ReviewContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ReviewContext;

internal sealed class ReviewMediaConfiguration : IEntityTypeConfiguration<ReviewMedia>
{
    public void Configure(EntityTypeBuilder<ReviewMedia> builder)
    {
        builder.ToTable("review_images");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ReviewId)
            .HasColumnName("review_id")
            .IsRequired();

        builder.Property(m => m.Url)
            .HasColumnName("image_url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}

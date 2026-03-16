using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.CatalogContext.Aggregates.Categories;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.CatalogContext;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => CategoryId.From(value));
        
        builder.Property(c => c.ParentId)
            .HasColumnName("parent_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? CategoryId.From(value.Value) : null);

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .IsRequired();
        
        builder.Property(c => c.Description)
            .HasColumnName("description")
            .IsRequired();

        builder.ComplexProperty(c => c.Slug, slugBuilder =>
        {
            slugBuilder.Property(s => s.Value)
                .HasColumnName("slug")
                .IsRequired();
        });

        builder.ComplexProperty(c => c.IconStorageRef, iconStorageRefBuilder =>
        {
            iconStorageRefBuilder.Property(s => s.PublicId)
                .HasColumnName("icon_public_id");
            
            iconStorageRefBuilder.Property(s => s.Folder)
                .HasColumnName("icon_folder");
        });

        builder.ComplexProperty(c => c.IconInfo, iconInfoBuilder =>
        {
            iconInfoBuilder.Property(i => i.SecureUrl)
                .HasColumnName("icon_secure_url")
                .IsRequired();
            
            iconInfoBuilder.Property(i => i.FileName)
                .HasColumnName("icon_file_name");
            
            iconInfoBuilder.Property(i => i.Bytes)
                .HasColumnName("icon_bytes");
            
            iconInfoBuilder.Property(i => i.Format)
                .HasColumnName("icon_format");
            
            iconInfoBuilder.Property(i => i.Width)
                .HasColumnName("icon_width");
            
            iconInfoBuilder.Property(i => i.Height)
                .HasColumnName("icon_height");
            
            iconInfoBuilder.Property(i => i.DurationSeconds)
                .HasColumnName("icon_duration_seconds");

            iconInfoBuilder.Ignore(i => i.IsVideo);
            iconInfoBuilder.Ignore(i => i.IsImage);
        });
        
        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
        
        builder.Property(c => c.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();
        
        builder.ComplexProperty(c => c.Path, pathBuilder =>
        {
            pathBuilder.Property(p => p.Value)
                .HasColumnName("path")
                .IsRequired();    
        });
        
        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        builder.Property(c => c.ModifiedAt)
            .HasColumnName("modified_at");
        
        builder.HasOne(c => c.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class DisputeResponseTemplateConfiguration : IEntityTypeConfiguration<DisputeResponseTemplate>
{
    public void Configure(EntityTypeBuilder<DisputeResponseTemplate> builder)
    {
        builder.ToTable("dispute_response_templates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Category)
            .HasColumnName("category")
            .HasMaxLength(50);

        builder.Property(t => t.Subject)
            .HasColumnName("subject")
            .HasMaxLength(255);

        builder.Property(t => t.Body)
            .HasColumnName("body")
            .IsRequired();

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}

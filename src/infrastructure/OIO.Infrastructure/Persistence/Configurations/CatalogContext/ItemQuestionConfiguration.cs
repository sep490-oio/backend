using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.CatalogContext;

internal sealed class ItemQuestionConfiguration : IEntityTypeConfiguration<ItemQuestion>
{
    public void Configure(EntityTypeBuilder<ItemQuestion> builder)
    {
        builder.ToTable("item_questions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(
                x => x.Value,
                x => ItemQuestionId.From(x));

        builder.Property(q => q.ItemId)
            .HasColumnName("item_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => ItemId.From(x));

        builder.Property(q => q.AskerId)
            .HasColumnName("asker_id")
            .IsRequired()
            .HasConversion(x => x.Value, x => UserId.From(x));

        builder.Property(q => q.Question)
            .HasColumnName("question")
            .IsRequired();

        builder.Property(q => q.Answer)
            .HasColumnName("answer");

        builder.Property(q => q.AnsweredAt)
            .HasColumnName("answered_at");

        builder.Property(q => q.IsPublic)
            .HasColumnName("is_public")
            .HasDefaultValue(true);

        builder.Property(q => q.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Moderation fields
        builder.Property(q => q.HiddenByAdminId)
            .HasColumnName("hidden_by_admin_id")
            .HasConversion(
                x => x != null ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? UserId.From(x.Value) : null);

        builder.Property(q => q.HiddenAt)
            .HasColumnName("hidden_at");

        builder.Property(q => q.HiddenReason)
            .HasColumnName("hidden_reason")
            .HasMaxLength(500);
    }
}
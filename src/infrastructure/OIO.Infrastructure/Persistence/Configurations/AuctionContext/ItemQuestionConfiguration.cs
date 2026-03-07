using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

internal sealed class ItemQuestionConfiguration : IEntityTypeConfiguration<ItemQuestion>
{
    public void Configure(EntityTypeBuilder<ItemQuestion> builder)
    {
        builder.ToTable("item_questions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => ItemQuestionId.From(x));

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

        // ==================== Ignore ====================
        builder.Ignore(q => q.IsAnswered);
    }
}
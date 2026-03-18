using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;

namespace OIO.Infrastructure.Persistence.Configurations.PaymentContext;

internal sealed class EscrowConfiguration : IEntityTypeConfiguration<Escrow>
{
    public void Configure(EntityTypeBuilder<Escrow> builder)
    {
        builder.ToTable("escrows");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(e => e.HoldTransactionId)
            .HasColumnName("hold_transaction_id");

        builder.Property(e => e.ReleaseTransactionId)
            .HasColumnName("release_transaction_id");

        builder.ComplexProperty(e => e.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            money.ComplexProperty(m => m.Currency, c =>
            {
                c.Property(x => x.Id)
                    .HasColumnName("amount_currency")
                    .HasMaxLength(3);

                c.Ignore(x => x.Symbol);
            });
        });

        builder.Property(e => e.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("VND");

        builder.ComplexProperty(e => e.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.Property(e => e.HeldAt)
            .HasColumnName("held_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.ReleasedAt)
            .HasColumnName("released_at");

        builder.ComplexProperty(e => e.ReleasedTo, releasedToBuilder =>
        {
            releasedToBuilder.Property(r => r.Id)
                .HasColumnName("released_to")
                .HasMaxLength(20);
        });

        // Navigation
        builder.HasMany(e => e.ReleaseEvents)
            .WithOne(re => re.Escrow)
            .HasForeignKey(re => re.EscrowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

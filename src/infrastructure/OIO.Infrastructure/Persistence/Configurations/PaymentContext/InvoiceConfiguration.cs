using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.PaymentContext.Aggregates.Invoices;

namespace OIO.Infrastructure.Persistence.Configurations.PaymentContext;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);

        builder.ComplexProperty(i => i.InvoiceNumber, inv =>
        {
            inv.Property(x => x.Value)
                .HasColumnName("invoice_number")
                .HasMaxLength(50)
                .IsRequired()
                .HasComplexIndex(isUnique: true);
        });

        builder.Property(i => i.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(i => i.BuyerId)
            .HasColumnName("buyer_id")
            .IsRequired();

        builder.Property(i => i.SellerId)
            .HasColumnName("seller_id")
            .IsRequired();

        builder.Property(i => i.Subtotal)
            .HasColumnName("subtotal")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(i => i.TaxAmount)
            .HasColumnName("tax_amount")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m);

        builder.ComplexProperty(i => i.TotalAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("total_amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            money.ComplexProperty(m => m.Currency, c =>
            {
                c.Property(x => x.Id)
                    .HasColumnName("total_amount_currency")
                    .HasMaxLength(3);

                c.Ignore(x => x.Symbol);
            });
        });

        builder.Property(i => i.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("VND");

        builder.ComplexProperty(i => i.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20);
        });

        builder.Property(i => i.IssuedAt)
            .HasColumnName("issued_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(i => i.DueDate)
            .HasColumnName("due_date");

        builder.Property(i => i.PaidAt)
            .HasColumnName("paid_at");
    }
}

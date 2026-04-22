using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;

namespace OIO.Infrastructure.Persistence.Configurations.PaymentContext;

internal sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("payment_methods");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.ComplexProperty(p => p.Type, typeBuilder =>
        {
            typeBuilder.Property(t => t.Id)
                .HasColumnName("type")
                .HasMaxLength(50)
                .IsRequired();
        });

        builder.Property(p => p.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50);

        builder.ComplexProperty(p => p.Card, cardBuilder =>
        {
            cardBuilder.Property(c => c.HolderName)
                .HasColumnName("holder_name")
                .HasMaxLength(100);

            cardBuilder.Property(c => c.LastFour)
                .HasColumnName("last_four")
                .HasMaxLength(4);

            cardBuilder.Property(c => c.ExpiryMonth)
                .HasColumnName("expiry_month");

            cardBuilder.Property(c => c.ExpiryYear)
                .HasColumnName("expiry_year");
        });

        builder.Property(p => p.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.IsVerified)
            .HasColumnName("is_verified")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(p => p.TokenReference)
            .HasColumnName("token_reference")
            .HasMaxLength(255);

        // VNPay Token fields
        builder.Property(p => p.VnPayToken)
            .HasColumnName("vnpay_token")
            .HasMaxLength(64);

        builder.Property(p => p.MaskedCardNumber)
            .HasColumnName("masked_card_number")
            .HasMaxLength(25);

        builder.Property(p => p.VnPayCardType)
            .HasColumnName("vnpay_card_type")
            .HasMaxLength(5);

        builder.Property(p => p.BankCode)
            .HasColumnName("bank_code")
            .HasMaxLength(20);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Indexes
        builder.HasIndex(p => new { p.UserId, p.IsActive })
            .HasDatabaseName("idx_payment_methods_user_active");

        builder.HasComplexCompositeIndex(
            columns: x => new { x.UserId, x.Type.Id, x.Provider, x.Card.LastFour, x.Card.ExpiryMonth, x.Card.ExpiryYear },
            filter: "is_active = true AND type IN ('credit_card','debit_card')",
            isUnique: true,
            indexName: "uq_payment_methods_user_card_active");
        
        builder.HasComplexCompositeIndex(
            columns: x => new { x.UserId, x.Type.Id, x.Provider, x.Card.HolderName },
            filter: "is_active = true AND type = 'e_wallet'",
            isUnique: true,
            indexName: "uq_payment_methods_user_e-wallet_active");
        
        builder.HasComplexCompositeIndex(
                columns: x => new { x.UserId, x.Type.Id, x.Provider, x.MaskedCardNumber, x.Card.LastFour },
                filter: "is_active = true AND type = 'bank_account'",
                isUnique: true,
                indexName: "uq_payment_methods_user_bank_active");

        builder.HasIndex(p => new { p.UserId, p.VnPayToken })
            .HasDatabaseName("idx_payment_methods_user_token")
            .HasFilter("vnpay_token IS NOT NULL");
        
        // VnPay card identity = (user, bank_code, masked_card_number). The
        // vnp_token VNPay issues is NOT stable across re-links for the same
        // physical card — keying on token would accept duplicates.
        builder.HasIndex(x => new { x.UserId, x.BankCode, x.MaskedCardNumber })
            .HasDatabaseName("uq_payment_methods_user_vnpay_active")
            .IsUnique()
            .HasFilter("is_active = true AND type = 'vnpay' AND masked_card_number IS NOT NULL");
    }
}

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
                .HasMaxLength(30)
                .IsRequired();
        });

        builder.Property(p => p.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50);

        builder.ComplexProperty(p => p.Card, card =>
        {
            card.Property(c => c.LastFour)
                .HasColumnName("last_four")
                .HasMaxLength(4);

            card.Property(c => c.ExpiryMonth)
                .HasColumnName("expiry_month");

            card.Property(c => c.ExpiryYear)
                .HasColumnName("expiry_year");

            card.Property(c => c.HolderName)
                .HasColumnName("holder_name")
                .HasMaxLength(200);
        });

        builder.Property(p => p.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false);

        builder.Property(p => p.IsVerified)
            .HasColumnName("is_verified")
            .HasDefaultValue(false);

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.TokenReference)
            .HasColumnName("token_reference")
            .HasMaxLength(255);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}

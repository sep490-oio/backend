using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ShippingContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ShippingContext;

internal sealed class ShippingProviderConfigConfiguration : IEntityTypeConfiguration<ShippingProviderConfig>
{
    public void Configure(EntityTypeBuilder<ShippingProviderConfig> builder)
    {
        builder.ToTable("shipping_provider_configs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ProviderCode)
            .HasColumnName("provider_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.ComplexProperty(c => c.Environment, envBuilder =>
        {
            envBuilder.Property(e => e.Id)
                .HasColumnName("environment")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.Property(c => c.ApiBaseUrl)
            .HasColumnName("api_base_url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.Credentials)
            .HasColumnName("credentials")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(c => c.CachedToken)
            .HasColumnName("cached_token");

        builder.Property(c => c.CachedTokenExpiresAt)
            .HasColumnName("cached_token_expires_at");

        builder.Property(c => c.WebhookSecret)
            .HasColumnName("webhook_secret");

        builder.ComplexProperty(c => c.PickAddress, pa =>
        {
            pa.Property(x => x.Name)
                .HasColumnName("pick_name")
                .HasMaxLength(200);

            pa.Property(x => x.Phone)
                .HasColumnName("pick_phone")
                .HasMaxLength(20);

            pa.Property(x => x.Address)
                .HasColumnName("pick_address")
                .HasMaxLength(500);

            pa.Property(x => x.Ward)
                .HasColumnName("pick_ward")
                .HasMaxLength(100);

            pa.Property(x => x.District)
                .HasColumnName("pick_district")
                .HasMaxLength(100);

            pa.Property(x => x.Province)
                .HasColumnName("pick_province")
                .HasMaxLength(100);

            pa.Property(x => x.CarrierAddressData)
                .HasColumnName("pick_carrier_address_data")
                .HasColumnType("jsonb");
        });

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(c => c.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(c => c.ModifiedAt)
            .HasColumnName("modified_at");

        // Indexes
        builder.HasIndex(c => c.ProviderCode)
            .IsUnique()
            .HasDatabaseName("uq_shipping_provider_configs_code");
    }
}

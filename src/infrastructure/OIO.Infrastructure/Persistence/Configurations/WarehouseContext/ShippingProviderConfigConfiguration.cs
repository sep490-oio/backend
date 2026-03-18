using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.WarehouseContext;

internal sealed class ShippingProviderConfigConfiguration : IEntityTypeConfiguration<ShippingProviderConfig>
{
    public void Configure(EntityTypeBuilder<ShippingProviderConfig> builder)
    {
        builder.ToTable("shipping_provider_configs", t =>
        {
            t.HasCheckConstraint(
                "chk_shipping_provider_configs_cached_token",
                "(cached_token IS NULL AND cached_token_expires_at IS NULL) OR " +
                "(cached_token IS NOT NULL AND cached_token_expires_at IS NOT NULL)");
        });

        // ==================== Primary Key ====================
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, v => ShippingProviderConfigId.From(v));

        // ==================== Properties ====================
        builder.Property(e => e.ProviderCode)
            .HasColumnName("provider_code")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(x => x.Id, v => ShippingProviderCode.FromId(v).GetValueOrThrow());

        builder.Property(e => e.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Environment)
            .HasColumnName("environment")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(x => x.Id, v => ShippingEnvironment.FromId(v).GetValueOrThrow());

        builder.Property(e => e.ApiBaseUrl)
            .HasColumnName("api_base_url")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Credentials)
            .HasColumnName("credentials")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(x => x.RawJson, v => ShippingCredentials.From(v));

        builder.Property(e => e.CachedToken)
            .HasColumnName("cached_token")
            .HasMaxLength(500);

        builder.Property(e => e.CachedTokenExpiresAt)
            .HasColumnName("cached_token_expires_at");

        builder.Property(e => e.WebhookSecret)
            .HasColumnName("webhook_secret")
            .HasMaxLength(255);

        builder.Property(e => e.PickName)
            .HasColumnName("pick_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PickPhone)
            .HasColumnName("pick_phone")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.PickAddress)
            .HasColumnName("pick_address")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.PickWard)
            .HasColumnName("pick_ward")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PickDistrict)
            .HasColumnName("pick_district")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PickProvince)
            .HasColumnName("pick_province")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PickCarrierAddressData)
            .HasColumnName("pick_carrier_address_data")
            .HasColumnType("jsonb")
            .HasConversion(
                x => x == null ? null : x.RawJson,
                v => v == null ? null : CarrierAddressData.From(v));

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.ModifiedAt)
            .HasColumnName("modified_at");

        // ==================== Indexes ====================
        builder.HasIndex(e => e.ProviderCode)
            .IsUnique()
            .HasDatabaseName("idx_unique_shipping_provider_configs_provider_code");

        builder.HasIndex(e => e.IsDefault)
            .HasDatabaseName("idx_shipping_provider_configs_default_active")
            .HasFilter("is_default = TRUE AND is_active = TRUE");

        // ==================== Ignore ====================
        builder.Ignore(e => e.DomainEvents);
    }
}
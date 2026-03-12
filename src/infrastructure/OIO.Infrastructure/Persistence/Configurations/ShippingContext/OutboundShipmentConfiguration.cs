using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ShippingContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ShippingContext;

internal sealed class OutboundShipmentConfiguration : IEntityTypeConfiguration<OutboundShipment>
{
    public void Configure(EntityTypeBuilder<OutboundShipment> builder)
    {
        builder.ToTable("outbound_shipments");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(s => s.WarehouseItemId)
            .HasColumnName("warehouse_item_id")
            .IsRequired();

        builder.Property(s => s.ProviderCode)
            .HasColumnName("provider_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.ClientOrderCode)
            .HasColumnName("client_order_code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.CarrierTrackingNumber)
            .HasColumnName("carrier_tracking_number")
            .HasMaxLength(100);

        builder.Property(s => s.ShippingLabelUrl)
            .HasColumnName("shipping_label_url")
            .HasMaxLength(500);

        builder.Property(s => s.ShippingMethod)
            .HasColumnName("shipping_method")
            .HasMaxLength(50);

        builder.Property(s => s.RecipientCarrierAddressData)
            .HasColumnName("recipient_carrier_address_data")
            .HasColumnType("jsonb");

        builder.ComplexProperty(s => s.Package, pkg =>
        {
            pkg.Property(x => x.WeightGrams)
                .HasColumnName("weight_grams")
                .IsRequired();

            pkg.Property(x => x.LengthCm)
                .HasColumnName("length_cm");

            pkg.Property(x => x.WidthCm)
                .HasColumnName("width_cm");

            pkg.Property(x => x.HeightCm)
                .HasColumnName("height_cm");

            pkg.Ignore(x => x.VolumeCm3);
        });

        builder.ComplexProperty(s => s.Cost, cost =>
        {
            cost.Property(x => x.ShippingFee)
                .HasColumnName("shipping_fee")
                .HasColumnType("numeric(18,2)");

            cost.Property(x => x.InsuranceValue)
                .HasColumnName("insurance_value")
                .HasColumnType("numeric(18,2)");

            cost.Property(x => x.CodAmount)
                .HasColumnName("cod_amount")
                .HasColumnType("numeric(18,2)");
        });

        builder.Property(s => s.PaymentTypeId)
            .HasColumnName("payment_type_id");

        builder.Property(s => s.HandlingNote)
            .HasColumnName("handling_note");

        builder.Property(s => s.ExtraData)
            .HasColumnName("extra_data")
            .HasColumnType("jsonb");

        builder.ComplexProperty(s => s.Status, statusBuilder =>
        {
            statusBuilder.Property(x => x.Id)
                .HasColumnName("status")
                .HasMaxLength(30)
                .IsRequired();
        });

        builder.Property(s => s.PackedBy)
            .HasColumnName("packed_by");

        builder.Property(s => s.PackedAt)
            .HasColumnName("packed_at");

        builder.Property(s => s.DispatchedAt)
            .HasColumnName("dispatched_at");

        builder.Property(s => s.EstimatedDeliveryAt)
            .HasColumnName("estimated_delivery_at");

        builder.Property(s => s.DeliveredAt)
            .HasColumnName("delivered_at");

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(s => s.ModifiedAt)
            .HasColumnName("modified_at");

        // Indexes
        builder.HasIndex(s => s.OrderId)
            .HasDatabaseName("idx_outbound_shipments_order");

        builder.HasIndex(s => s.WarehouseItemId)
            .HasDatabaseName("idx_outbound_shipments_warehouse_item");

        builder.HasIndex(s => s.CarrierTrackingNumber)
            .HasDatabaseName("idx_outbound_shipments_tracking");
    }
}

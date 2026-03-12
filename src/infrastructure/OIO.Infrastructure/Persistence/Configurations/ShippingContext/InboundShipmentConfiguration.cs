using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ShippingContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ShippingContext;

internal sealed class InboundShipmentConfiguration : IEntityTypeConfiguration<InboundShipment>
{
    public void Configure(EntityTypeBuilder<InboundShipment> builder)
    {
        builder.ToTable("inbound_shipments");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ItemId)
            .HasColumnName("item_id")
            .IsRequired();

        builder.Property(s => s.SellerId)
            .HasColumnName("seller_id")
            .IsRequired();

        builder.Property(s => s.AuctionId)
            .HasColumnName("auction_id")
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

        builder.ComplexProperty(s => s.Sender, sender =>
        {
            sender.Property(x => x.Name)
                .HasColumnName("sender_name")
                .HasMaxLength(200);

            sender.Property(x => x.Phone)
                .HasColumnName("sender_phone")
                .HasMaxLength(20);

            sender.Property(x => x.Address)
                .HasColumnName("sender_address")
                .HasMaxLength(500);

            sender.Property(x => x.Ward)
                .HasColumnName("sender_ward")
                .HasMaxLength(100);

            sender.Property(x => x.District)
                .HasColumnName("sender_district")
                .HasMaxLength(100);

            sender.Property(x => x.Province)
                .HasColumnName("sender_province")
                .HasMaxLength(100);

            sender.Property(x => x.CarrierAddressData)
                .HasColumnName("sender_carrier_address_data")
                .HasColumnType("jsonb");
        });

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

        builder.Property(s => s.Notes)
            .HasColumnName("notes");

        builder.Property(s => s.ExpectedArrivalAt)
            .HasColumnName("expected_arrival_at");

        builder.Property(s => s.ArrivedAt)
            .HasColumnName("arrived_at");

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(s => s.ModifiedAt)
            .HasColumnName("modified_at");

        // Navigation: 1:1 with WarehouseItem
        builder.HasOne(s => s.WarehouseItem)
            .WithOne(w => w.InboundShipment)
            .HasForeignKey<WarehouseItem>(w => w.InboundShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.ItemId)
            .HasDatabaseName("idx_inbound_shipments_item");

        builder.HasIndex(s => s.SellerId)
            .HasDatabaseName("idx_inbound_shipments_seller");

        builder.HasIndex(s => s.CarrierTrackingNumber)
            .HasDatabaseName("idx_inbound_shipments_tracking");
    }
}

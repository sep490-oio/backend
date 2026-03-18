using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;

namespace OIO.Application.Context.WarehouseContext.Mappings;

internal static class OutboundShipmentMappings
{
    public static OutboundShipmentDto ToDto(this OutboundShipment shipment) =>
        new(
            Id:                   shipment.Id.Value,
            OrderId:              shipment.OrderId.Value,
            WarehouseItemId:      shipment.WarehouseItemId.Value,
            ProviderCode:         shipment.ProviderCode.Id,
            ClientOrderCode:      shipment.ClientOrderCode,
            CarrierTrackingNumber: shipment.CarrierTrackingNumber,
            ShippingLabelUrl:     shipment.ShippingLabelUrl,
            ShippingMethod:       shipment.ShippingMethod,
            WeightGrams:          shipment.Dimensions.WeightGrams,
            LengthCm:             shipment.Dimensions.LengthCm,
            WidthCm:              shipment.Dimensions.WidthCm,
            HeightCm:             shipment.Dimensions.HeightCm,
            ShippingFee:          shipment.ShippingFee,
            InsuranceValue:       shipment.InsuranceValue,
            CodAmount:            shipment.CodAmount,
            Status:               shipment.Status.Id,
            EstimatedDeliveryAt:  shipment.EstimatedDeliveryAt,
            PackedAt:             shipment.PackedAt,
            DispatchedAt:         shipment.DispatchedAt,
            DeliveredAt:          shipment.DeliveredAt,
            CreatedAt:            shipment.CreatedAt,
            ModifiedAt:           shipment.ModifiedAt,
            TrackingEvents:        shipment.TrackingEvents.Select(e => e.ToDto()).ToList());
}
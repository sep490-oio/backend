using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;

namespace OIO.Application.Context.WarehouseContext.Mappings;

internal static class InboundShipmentMappings
{
    public static InboundShipmentDto ToDto(this InboundShipment shipment) =>
        new(
            Id:                    shipment.Id.Value,
            ItemId:                shipment.ItemId,
            SellerId:              shipment.SellerId.Value,

            ProviderCode:          shipment.ProviderCode.Id,
            ShipmentMode:          shipment.ShipmentMode.Id,
            ExternalCarrierName:   shipment.ExternalCarrierName,

            ClientOrderCode:       shipment.ClientOrderCode,
            CarrierTrackingNumber: shipment.CarrierTrackingNumber,

            SenderName:            shipment.SenderName,
            SenderPhone:           shipment.SenderPhone,
            SenderAddress:         shipment.SenderAddress,
            SenderWard:            shipment.SenderWard,
            SenderDistrict:        shipment.SenderDistrict,
            SenderProvince:        shipment.SenderProvince,

            WeightGrams:           shipment.Dimensions.WeightGrams,
            LengthCm:              shipment.Dimensions.LengthCm,
            WidthCm:               shipment.Dimensions.WidthCm,
            HeightCm:              shipment.Dimensions.HeightCm,

            ShippingFee:           shipment.ShippingFee,
            InsuranceValue:        shipment.InsuranceValue,

            Status:                shipment.Status.Id,
            Notes:                 shipment.Notes,

            ExpectedArrivalAt:     shipment.ExpectedArrivalAt,
            ArrivedAt:             shipment.ArrivedAt,

            CreatedAt:             shipment.CreatedAt,
            ModifiedAt:            shipment.ModifiedAt,

            TrackingEvents:        shipment.TrackingEvents
                                      .Select(e => e.ToDto())
                                      .ToList()
        );
}
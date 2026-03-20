using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.WarehouseContext.Aggregates;

namespace OIO.Application.Context.WarehouseContext.Mappings;

internal static class ShipmentTrackingEventMappings
{
    public static ShipmentTrackingEventDto ToDto(this ShipmentTrackingEvent e) =>
        new(
            Id:                e.Id.Value,
            ProviderCode:      e.ProviderCode.Id,
            CarrierStatusRaw:  e.CarrierStatusRaw,
            CarrierStatusDesc: e.CarrierStatusDesc,
            NormalizedStatus:  e.NormalizedStatus.Id,
            Location:          e.Location,
            ReasonCode:        e.ReasonCode,
            ReasonDescription: e.ReasonDescription,
            EventTime:         e.EventTime,
            CreatedAt:         e.CreatedAt);
}
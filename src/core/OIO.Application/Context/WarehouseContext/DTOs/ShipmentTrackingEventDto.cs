namespace OIO.Application.Context.WarehouseContext.DTOs;

public sealed record ShipmentTrackingEventDto(
    Guid    Id,
    string  ProviderCode,
    string  CarrierStatusRaw,
    string? CarrierStatusDesc,
    string  NormalizedStatus,
    string? Location,
    string? ReasonCode,
    string? ReasonDescription,
    DateTime EventTime,
    DateTime CreatedAt);
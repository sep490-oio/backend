namespace OIO.Application.Context.AuctionContext.Queries.Admins.GetAdminItemLogistics;

public sealed record AdminItemLogisticsEventDto(
    string EventType, // e.g. "INBOUND_BOOKED", "WAREHOUSE_STORED", "OUTBOUND_BOOKED", "DELIVERED"
    DateTime Timestamp,
    string Description,
    string? Location,
    string? Carrier,
    string? TrackingCode,
    string? ReferenceId
);

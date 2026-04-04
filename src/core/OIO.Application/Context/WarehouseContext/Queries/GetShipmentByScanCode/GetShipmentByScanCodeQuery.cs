using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;

namespace OIO.Application.Context.WarehouseContext.Queries.GetShipmentByScanCode;

public sealed record GetShipmentByScanCodeQuery(string ScanCode) : IQuery<List<InboundShipmentDto>>;

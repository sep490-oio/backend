using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetShipmentByScanCode;

internal sealed class GetShipmentByScanCodeQueryHandler
    : IQueryHandler<GetShipmentByScanCodeQuery, InboundShipmentDto>
{
    private readonly IDbContext _dbContext;

    public GetShipmentByScanCodeQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<InboundShipmentDto, Error>> Handle(
        GetShipmentByScanCodeQuery request,
        CancellationToken          cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ScanCode))
        {
            return Error.Validation("ScanCode", "GetShipment.ScanCodeEmpty", "Scan code cannot be empty.");
        }

        var shipment = await _dbContext.Set<InboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.ClientOrderCode == request.ScanCode || s.CarrierTrackingNumber == request.ScanCode,
                cancellationToken);

        if (shipment is null)
        {
            return Error.NotFound("InboundShipment.NotFoundByScanCode", $"No shipment found matching scan code '{request.ScanCode}'.");
        }

        return shipment.ToDto();
    }
}

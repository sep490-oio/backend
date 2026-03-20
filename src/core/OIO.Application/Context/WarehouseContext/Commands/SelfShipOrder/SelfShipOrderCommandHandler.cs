using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.SelfShipOrder;

internal sealed class SelfShipOrderCommandHandler
    : ICommandHandler<SelfShipOrderCommand, OutboundShipmentDto>
{
    private readonly IDbContext  _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock      _clock;

    public SelfShipOrderCommandHandler(
        IDbContext  dbContext,
        IUnitOfWork unitOfWork,
        IClock      clock)
    {
        _dbContext  = dbContext;
        _unitOfWork = unitOfWork;
        _clock      = clock;
    }

    public async Task<Result<OutboundShipmentDto, Error>> Handle(
        SelfShipOrderCommand request,
        CancellationToken    cancellationToken)
    {
        var now = _clock.UtcNow;

        // 1. Check if an outbound shipment already exists for this order
        var existingShipment = await _dbContext.Set<OutboundShipment>()
            .AnyAsync(s => s.OrderId == OrderId.From(request.OrderId), cancellationToken);

        if (existingShipment)
            return WarehouseErrors.OutboundShipment.AlreadyExists(request.OrderId.ToString());

        // 2. Create the OutboundShipment in SellerSelfShip mode
        var clientOrderCode = $"SELF-{Guid.NewGuid():N}"[..20];
        
        var dimensions = PackageDimensions.Create(
            weightGrams: request.WeightGrams,
            lengthCm:    null,
            widthCm:     null,
            heightCm:    null).Value;

        var shipment = OutboundShipment.Create(
            orderId:             OrderId.From(request.OrderId),
            warehouseItemId:     null, // No warehouse item for self-ship
            providerCode:        ShippingProviderCode.External,
            clientOrderCode:     clientOrderCode,
            dimensions:          dimensions,
            now:                 now,
            shipmentMode:        OutboundShipmentMode.SellerSelfShip,
            externalCarrierName: request.ExternalCarrierName,
            shippingMethod:      request.ShippingMethod,
            insuranceValue:      request.InsuranceValue);

        // 3. Record the tracking number immediately
        var shipResult = shipment.RecordSellerShipped(request.CarrierTrackingNumber, now);
        if (shipResult.IsFailure) return shipResult.Error;

        // 4. Persist
        _dbContext.Insert(shipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return shipment.ToDto();
    }
}

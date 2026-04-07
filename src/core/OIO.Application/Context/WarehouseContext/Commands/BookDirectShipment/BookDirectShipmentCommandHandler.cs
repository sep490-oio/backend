using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Shipping;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.BookDirectShipment;

internal sealed class BookDirectShipmentCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IShippingService shippingService)
    : ICommandHandler<BookDirectShipmentCommand, OutboundShipmentDto>
{
    public async Task<Result<OutboundShipmentDto, Error>> Handle(
        BookDirectShipmentCommand request,
        CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await dbContext.GetByIdAsync<Order, OrderId>(orderId, cancellationToken: cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(orderId);

        // 1. Get GHN config
        var config = await dbContext.Set<ShippingProviderConfig>()
            .FirstOrDefaultAsync(c => c.ProviderCode == ShippingProviderCode.Ghn, cancellationToken);
            
        if (config is null)
            return Error.NotFound("ShippingProvider.NotFound", "GHN provider config not found.");

        // 2. Prepare request for IShippingService
        var bookingRequest = new BookShipmentRequest
        {
            ClientOrderCode = order.OrderNumber.Value,
            RecipientName = order.Shipping.RecipientName ?? string.Empty,
            RecipientPhone = order.Shipping.Phone ?? string.Empty,
            RecipientAddress = order.Shipping.Address,
            RecipientWard = order.Shipping.Ward ?? string.Empty,
            RecipientDistrict = order.Shipping.District ?? string.Empty,
            RecipientProvince = order.Shipping.City ?? string.Empty,
            RecipientCarrierAddressDataJson = request.RecipientCarrierAddressDataJson,
            
            SenderName = request.SenderName,
            SenderPhone = request.SenderPhone,
            SenderAddress = request.SenderAddress,
            SenderWard = request.SenderWard,
            SenderDistrict = request.SenderDistrict,
            SenderProvince = request.SenderProvince,
            SenderCarrierAddressDataJson = request.SenderCarrierAddressDataJson,
            
            WeightGrams = request.WeightGrams,
            LengthCm = request.LengthCm,
            WidthCm = request.WidthCm,
            HeightCm = request.HeightCm,
            InsuranceValue = request.InsuranceValue,
            
            Items = new List<BookShipmentItem>
            {
                new()
                {
                    Name = request.ItemName ?? "Sản phẩm đấu giá",
                    Code = order.OrderNumber.Value,
                    Quantity = 1,
                    Price = request.ItemPrice ?? order.Pricing.TotalAmount.Amount,
                    WeightGrams = request.WeightGrams
                }
            },
            GhnPaymentTypeId = request.GhnPaymentTypeId,
            GhnHandlingNote = request.GhnHandlingNote
        };

        // 3. Book via service
        var bookingResult = await shippingService.BookShipmentAsync(
            ShippingProviderCode.Ghn.Id,
            bookingRequest,
            config,
            cancellationToken);

        if (bookingResult.IsFailure)
            return bookingResult.Error;

        var result = bookingResult.Value;

        // 4. Create internal OutboundShipment
        var dimensionsResult = PackageDimensions.Create(
            request.WeightGrams,
            request.LengthCm,
            request.WidthCm,
            request.HeightCm);

        if (dimensionsResult.IsFailure)
            return dimensionsResult.Error;

        var recipientCarrierData = CarrierAddressData.From(request.RecipientCarrierAddressDataJson);

        var shipment = OutboundShipment.Create(
            orderId: order.Id,
            warehouseItemId: null,
            providerCode: ShippingProviderCode.Ghn,
            clientOrderCode: order.OrderNumber.Value,
            dimensions: dimensionsResult.Value,
            now: DateTime.UtcNow,
            shipmentMode: OutboundShipmentMode.SellerSelfShip,
            shippingMethod: request.ShippingMethod,
            recipientCarrierAddressData: recipientCarrierData,
            shippingFee: result.ShippingFee,
            insuranceValue: request.InsuranceValue
        );

        // Record the booking result (tracking number, label, etc)
        var recordResult = shipment.RecordBooked(
            result.CarrierTrackingNumber,
            DateTime.UtcNow,
            result.ShippingLabelUrl,
            result.EstimatedDeliveryAt);

        if (recordResult.IsFailure)
            return recordResult.Error;

        dbContext.Set<OutboundShipment>().Add(shipment);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return shipment.ToDto();
    }
}

using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Shipping;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Services;

internal sealed record ItemShippingSelectionRequest(
    string SenderName,
    string SenderPhone,
    string SenderAddress,
    string SenderWard,
    string SenderDistrict,
    string SenderProvince,
    int WeightGrams,
    decimal InsuranceValue,
    string? ProviderCode = null,
    string? SenderCarrierAddressDataJson = null,
    int? LengthCm = null,
    int? WidthCm = null,
    int? HeightCm = null,
    string? ExternalTrackingNumber = null,
    string? ExternalCarrierName = null,
    string? Notes = null) : IHasValidate
{
    public ViolationsError Validate()
    {
        return ItemShippingSelectionRequest.Check()
            .WithOwnerName("ItemShippingSelection")
            .Field(SenderName).NotWhiteSpace()
            .Field(SenderPhone).NotWhiteSpace()
            .Field(SenderAddress).NotWhiteSpace()
            .Field(SenderWard).NotWhiteSpace()
            .Field(SenderDistrict).NotWhiteSpace()
            .Field(SenderProvince).NotWhiteSpace()
            .Field(WeightGrams).GreaterThan(0)
            .Field(InsuranceValue).NonNegative();
    }
}

internal sealed class ItemShippingSelectionService(
    IDbContext dbContext,
    IShippingService shippingService)
{
    public async Task<Result<InboundShipment, Error>> ChooseAsync(
        Item item,
        UserId actorId,
        ItemShippingSelectionRequest request,
        DateTime nowUtc,
        decimal? declaredItemPrice,
        CancellationToken cancellationToken)
    {
        if (item.SellerId != actorId)
            return AuctionErrors.Item.NotOwnedByUser(item.Id, actorId);

        if (item.Status != ItemStatus.PendingVerify)
            return AuctionErrors.Item.InvalidState(item.Status.Id, "choose shipping");

        // Check for existing active shipments
        var hasActive = await dbContext.Set<InboundShipment>()
            .AnyAsync(s => s.ItemId == item.Id.Value &&
                           s.Status.Id != InboundShipmentStatus.Cancelled.Id &&
                           s.Status.Id != InboundShipmentStatus.Failed.Id,
                      cancellationToken);

        if (hasActive)
            return WarehouseErrors.InboundShipment.AlreadyExists(item.Id.Value.ToString());

        var dimensionsResult = PackageDimensions.Create(
            weightGrams: request.WeightGrams,
            lengthCm: request.LengthCm,
            widthCm: request.WidthCm,
            heightCm: request.HeightCm);
        if (dimensionsResult.IsFailure)
            return dimensionsResult.Error;

        var clientOrderCode = $"INB-{Guid.NewGuid():N}"[..20];
        var useExternalCarrier = !string.IsNullOrWhiteSpace(request.ExternalTrackingNumber);

        if (useExternalCarrier)
        {
            var externalShipmentResult = InboundShipment.Create(
                itemId: item.Id.Value,
                sellerId: actorId,
                providerCode: ShippingProviderCode.External,
                clientOrderCode: clientOrderCode,
                senderName: request.SenderName,
                senderPhone: request.SenderPhone,
                senderAddress: request.SenderAddress,
                senderWard: request.SenderWard,
                senderDistrict: request.SenderDistrict,
                senderProvince: request.SenderProvince,
                dimensions: dimensionsResult.Value,
                now: nowUtc,
                insuranceValue: request.InsuranceValue,
                notes: request.Notes);

            if (externalShipmentResult.IsFailure)
                return externalShipmentResult.Error;

            var externalShipment = externalShipmentResult.Value;

            var bookedResult = externalShipment.RecordBooked(request.ExternalTrackingNumber!, nowUtc);
            if (bookedResult.IsFailure)
                return bookedResult.Error;

            dbContext.Insert(externalShipment);
            return externalShipment;
        }

        ShippingProviderConfig? config;
        if (!string.IsNullOrWhiteSpace(request.ProviderCode))
        {
            var providerCode = ShippingProviderCode.FromId(request.ProviderCode);
            if (providerCode.HasNoValue)
                return WarehouseErrors.ShippingProvider.NotFound(request.ProviderCode);

            config = await dbContext.Set<ShippingProviderConfig>()
                .FirstOrDefaultAsync(
                    c => c.ProviderCode == providerCode.Value && c.IsActive,
                    cancellationToken);
            if (config is null)
                return WarehouseErrors.ShippingProvider.NotFound(request.ProviderCode);
        }
        else
        {
            config = await dbContext.Set<ShippingProviderConfig>()
                .FirstOrDefaultAsync(c => c.IsDefault && c.IsActive, cancellationToken);
            if (config is null)
                return WarehouseErrors.ShippingProvider.NoDefaultProvider;
        }

        var bookingRequest = new BookShipmentRequest
        {
            ClientOrderCode = clientOrderCode,
            RecipientName = config.PickName,
            RecipientPhone = config.PickPhone,
            RecipientAddress = config.PickAddress,
            RecipientWard = config.PickWard,
            RecipientDistrict = config.PickDistrict,
            RecipientProvince = config.PickProvince,
            RecipientCarrierAddressDataJson = config.PickCarrierAddressData?.RawJson,
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
            CodAmount = 0,
            Items =
            [
                new BookShipmentItem
                {
                    Name = item.Title.Value,
                    Code = clientOrderCode,
                    Quantity = 1,
                    Price = declaredItemPrice ?? request.InsuranceValue,
                    WeightGrams = request.WeightGrams
                }
            ]
        };

        var bookingResult = await shippingService.BookShipmentAsync(
            config.ProviderCode.Id,
            bookingRequest,
            config,
            cancellationToken);
        if (bookingResult.IsFailure)
            return bookingResult.Error;

        var booking = bookingResult.Value;
        var carrierShipmentResult = InboundShipment.Create(
            itemId: item.Id.Value,
            sellerId: actorId,
            providerCode: config.ProviderCode,
            clientOrderCode: clientOrderCode,
            senderName: request.SenderName,
            senderPhone: request.SenderPhone,
            senderAddress: request.SenderAddress,
            senderWard: request.SenderWard,
            senderDistrict: request.SenderDistrict,
            senderProvince: request.SenderProvince,
            dimensions: dimensionsResult.Value,
            now: nowUtc,
            senderCarrierAddressData: request.SenderCarrierAddressDataJson is not null
                ? CarrierAddressData.From(request.SenderCarrierAddressDataJson)
                : null,
            shippingFee: booking.ShippingFee,
            insuranceValue: request.InsuranceValue,
            notes: request.Notes,
            expectedArrivalAt: booking.EstimatedDeliveryAt);

        if (carrierShipmentResult.IsFailure)
            return carrierShipmentResult.Error;

        var carrierShipment = carrierShipmentResult.Value;

        var recordBookedResult = carrierShipment.RecordBooked(booking.CarrierTrackingNumber, nowUtc);
        if (recordBookedResult.IsFailure)
            return recordBookedResult.Error;

        dbContext.Insert(carrierShipment);
        return carrierShipment;
    }
}

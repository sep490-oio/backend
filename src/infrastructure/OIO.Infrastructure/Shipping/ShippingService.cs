using CSharpFunctionalExtensions;
using MediatR;
using OIO.Application.Abstractions.Shipping;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Shipping;

/// <summary>
/// Infrastructure implementation of IShippingService.
/// Maps Application-layer BookShipmentRequest → Infrastructure CreateShipmentRequest,
/// delegates to the correct IShippingProvider via IShippingProviderSelector.
/// </summary>
internal sealed class ShippingService : IShippingService
{
    private readonly IShippingProviderSelector _selector;

    public ShippingService(IShippingProviderSelector selector)
    {
        _selector = selector;
    }

    public async Task<Result<ShipmentBookingResult, Error>> BookShipmentAsync(
        string                 providerCode,
        BookShipmentRequest    request,
        ShippingProviderConfig config,
        CancellationToken      ct = default)
    {
        var providerResult = _selector.Select(providerCode);
        if (providerResult.IsFailure) return providerResult.Error;

        var infraRequest = new CreateShipmentRequest
        {
            ClientOrderCode = request.ClientOrderCode,

            RecipientName                  = request.RecipientName,
            RecipientPhone                 = request.RecipientPhone,
            RecipientAddress               = request.RecipientAddress,
            RecipientWard                  = request.RecipientWard,
            RecipientDistrict              = request.RecipientDistrict,
            RecipientProvince              = request.RecipientProvince,
            RecipientCarrierAddressDataJson = request.RecipientCarrierAddressDataJson,

            SenderName                  = request.SenderName,
            SenderPhone                 = request.SenderPhone,
            SenderAddress               = request.SenderAddress,
            SenderWard                  = request.SenderWard,
            SenderDistrict              = request.SenderDistrict,
            SenderProvince              = request.SenderProvince,
            SenderCarrierAddressDataJson = request.SenderCarrierAddressDataJson,

            WeightGrams    = request.WeightGrams,
            LengthCm       = request.LengthCm,
            WidthCm        = request.WidthCm,
            HeightCm       = request.HeightCm,
            InsuranceValue = request.InsuranceValue,
            CodAmount      = request.CodAmount,

            GhnPaymentTypeId = request.GhnPaymentTypeId,
            GhnHandlingNote  = request.GhnHandlingNote,
            ExtraDataJson    = request.ExtraDataJson,

            Items = request.Items.Select(i => new ShipmentItem
            {
                Name        = i.Name,
                Code        = i.Code,
                Quantity    = i.Quantity,
                Price       = i.Price,
                WeightGrams = i.WeightGrams
            }).ToList()
        };

        var result = await providerResult.Value.CreateShipmentAsync(infraRequest, config, ct);
        if (result.IsFailure) return result.Error;

        return new ShipmentBookingResult
        {
            CarrierTrackingNumber = result.Value.CarrierTrackingNumber,
            ShippingLabelUrl      = result.Value.ShippingLabelUrl,
            ShippingFee           = result.Value.ShippingFee,
            EstimatedDeliveryAt   = result.Value.EstimatedDeliveryAt
        };
    }

    public async Task<Result<Unit, Error>> CancelShipmentAsync(
        string                 providerCode,
        string                 carrierTrackingNumber,
        ShippingProviderConfig config,
        CancellationToken      ct = default)
    {
        var providerResult = _selector.Select(providerCode);
        if (providerResult.IsFailure) return providerResult.Error;

        return await providerResult.Value.CancelShipmentAsync(carrierTrackingNumber, config, ct);
    }

    public async Task<Result<decimal, Error>> CalculateFeeAsync(
        string                                            providerCode,
        OIO.Application.Abstractions.Shipping.CalculateFeeRequest request,
        ShippingProviderConfig                            config,
        CancellationToken                                 ct = default)
    {
        var providerResult = _selector.Select(providerCode);
        if (providerResult.IsFailure) return providerResult.Error;

        var infraRequest = new OIO.Infrastructure.Shipping.CalculateFeeRequest
        {
            WeightGrams                     = request.WeightGrams,
            InsuranceValue                  = request.InsuranceValue,
            CodAmount                       = request.CodAmount,
            RecipientDistrict               = request.RecipientDistrict,
            RecipientProvince               = request.RecipientProvince,
            RecipientCarrierAddressDataJson = request.RecipientCarrierAddressDataJson,
            LengthCm                        = request.LengthCm,
            WidthCm                         = request.WidthCm,
            HeightCm                        = request.HeightCm
        };

        return await providerResult.Value.CalculateFeeAsync(infraRequest, config, ct);
    }

    public async Task<Result<DateTime?, Error>> CalculateExpectedDeliveryTimeAsync(
        string                                                              providerCode,
        OIO.Application.Abstractions.Shipping.CalculateExpectedDeliveryTimeRequest  request,
        ShippingProviderConfig                                              config,
        CancellationToken                                                   ct = default)
    {
        var providerResult = _selector.Select(providerCode);
        if (providerResult.IsFailure) return providerResult.Error;

        var infraRequest = new OIO.Infrastructure.Shipping.CalculateExpectedDeliveryTimeRequest
        {
            SenderDistrict                  = request.SenderDistrict,
            SenderProvince                  = request.SenderProvince,
            SenderCarrierAddressDataJson    = request.SenderCarrierAddressDataJson,
            RecipientDistrict               = request.RecipientDistrict,
            RecipientProvince               = request.RecipientProvince,
            RecipientCarrierAddressDataJson = request.RecipientCarrierAddressDataJson
        };

        return await providerResult.Value.CalculateExpectedDeliveryTimeAsync(infraRequest, config, ct);
    }
}
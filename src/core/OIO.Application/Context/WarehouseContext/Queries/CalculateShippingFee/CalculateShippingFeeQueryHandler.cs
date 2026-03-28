using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Shipping;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.CalculateShippingFee;

internal sealed class CalculateShippingFeeQueryHandler : IRequestHandler<CalculateShippingFeeQuery, Result<decimal, Error>>
{
    private readonly IDbContext         _dbContext;
    private readonly IShippingService   _shippingService;

    public CalculateShippingFeeQueryHandler(IDbContext dbContext, IShippingService shippingService)
    {
        _dbContext       = dbContext;
        _shippingService = shippingService;
    }

    public async Task<Result<decimal, Error>> Handle(CalculateShippingFeeQuery request, CancellationToken cancellationToken)
    {
        // 1. Load provider config
        var providerCode = ShippingProviderCode.FromId(request.ProviderCode);
        if (providerCode.HasNoValue)
            return Error.NotFound("ShippingProviderConfig.NotFound", $"Config for provider '{request.ProviderCode}' was not found.");

        var config = await _dbContext.Set<ShippingProviderConfig>()
            .FirstOrDefaultAsync(c => c.ProviderCode == providerCode.Value, cancellationToken);

        if (config is null)
            return Error.NotFound("ShippingProviderConfig.NotFound", $"Config for provider '{request.ProviderCode}' was not found.");

        // 2. Resolve Recipient address
        var recipientDistrict               = request.RecipientDistrict;
        var recipientProvince               = request.RecipientProvince;
        var recipientCarrierAddressDataJson = request.RecipientCarrierAddressDataJson;

        if (request.RecipientIsWarehouse)
        {
            recipientDistrict               ??= config.PickDistrict;
            recipientProvince               ??= config.PickProvince;
            recipientCarrierAddressDataJson ??= config.PickCarrierAddressData?.RawJson;
        }
        else if (request.RecipientUserId.HasValue)
        {
            var recipientId   = UserId.From(request.RecipientUserId.Value);
            var recipientAddr = await _dbContext.Set<UserAddress>()
                .FirstOrDefaultAsync(a => a.UserId == recipientId && a.IsDefault, cancellationToken);

            if (recipientAddr is null)
                return Error.NotFound("UserAddress.NotFound", $"No default address found for recipient user '{request.RecipientUserId}'.");

            recipientDistrict ??= recipientAddr.Address.District;
            recipientProvince ??= recipientAddr.Address.City;
        }

        if (string.IsNullOrWhiteSpace(recipientDistrict) || string.IsNullOrWhiteSpace(recipientProvince))
            return Error.Validation("RecipientAddress", "Ghn.Address.Missing",
                "Recipient district and province are required. Provide RecipientIsWarehouse, RecipientUserId, or explicit RecipientDistrict/RecipientProvince.");

        // 3. Build service request and delegate
        var feeRequest = new OIO.Application.Abstractions.Shipping.CalculateFeeRequest
        {
            WeightGrams                     = request.WeightGrams,
            InsuranceValue                  = request.InsuranceValue,
            CodAmount                       = request.CodAmount,
            RecipientDistrict               = recipientDistrict,
            RecipientProvince               = recipientProvince,
            RecipientCarrierAddressDataJson = recipientCarrierAddressDataJson,
            LengthCm                        = request.LengthCm,
            WidthCm                         = request.WidthCm,
            HeightCm                        = request.HeightCm
        };

        return await _shippingService.CalculateFeeAsync(
            request.ProviderCode,
            feeRequest,
            config,
            cancellationToken);
    }
}

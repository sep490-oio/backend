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

namespace OIO.Application.Context.WarehouseContext.Queries.CalculateExpectedDeliveryTime;

internal sealed class CalculateExpectedDeliveryTimeQueryHandler : IRequestHandler<CalculateExpectedDeliveryTimeQuery, Result<DateTime?, Error>>
{
    private readonly IDbContext         _dbContext;
    private readonly IShippingService   _shippingService;

    public CalculateExpectedDeliveryTimeQueryHandler(IDbContext dbContext, IShippingService shippingService)
    {
        _dbContext       = dbContext;
        _shippingService = shippingService;
    }

    public async Task<Result<DateTime?, Error>> Handle(CalculateExpectedDeliveryTimeQuery request, CancellationToken cancellationToken)
    {
        // 1. Load provider config
        var providerCode = ShippingProviderCode.FromId(request.ProviderCode);
        if (providerCode.HasNoValue)
            return Error.NotFound("ShippingProviderConfig.NotFound", $"Config for provider '{request.ProviderCode}' was not found.");

        var config = await _dbContext.Set<ShippingProviderConfig>()
            .FirstOrDefaultAsync(c => c.ProviderCode == providerCode.Value, cancellationToken);

        if (config is null)
            return Error.NotFound("ShippingProviderConfig.NotFound", $"Config for provider '{request.ProviderCode}' was not found.");

        // 2. Resolve Sender address
        var senderDistrict               = request.SenderDistrict;
        var senderProvince               = request.SenderProvince;
        var senderCarrierAddressDataJson = request.SenderCarrierAddressDataJson;

        if (request.SenderUserId.HasValue)
        {
            var senderId   = UserId.From(request.SenderUserId.Value);
            var senderAddr = await _dbContext.Set<UserAddress>()
                .FirstOrDefaultAsync(a => a.UserId == senderId && a.IsDefault, cancellationToken);

            if (senderAddr is null)
                return Error.NotFound("UserAddress.NotFound", $"No default address found for sender user '{request.SenderUserId}'.");

            senderDistrict ??= senderAddr.Address.District;
            senderProvince ??= senderAddr.Address.City;
            // CarrierAddressDataJson not stored on UserAddress — leave null unless explicitly provided
        }

        if (string.IsNullOrWhiteSpace(senderDistrict) || string.IsNullOrWhiteSpace(senderProvince))
            return Error.Validation("SenderAddress", "Ghn.Address.Missing",
                "Sender district and province are required. Provide SenderUserId or explicit SenderDistrict/SenderProvince.");

        // 3. Resolve Recipient address
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

        // 4. Build service request and delegate
        var timeRequest = new OIO.Application.Abstractions.Shipping.CalculateExpectedDeliveryTimeRequest
        {
            SenderDistrict                  = senderDistrict,
            SenderProvince                  = senderProvince,
            SenderCarrierAddressDataJson    = senderCarrierAddressDataJson,
            RecipientDistrict               = recipientDistrict,
            RecipientProvince               = recipientProvince,
            RecipientCarrierAddressDataJson = recipientCarrierAddressDataJson
        };

        return await _shippingService.CalculateExpectedDeliveryTimeAsync(
            request.ProviderCode,
            timeRequest,
            config,
            cancellationToken);
    }
}

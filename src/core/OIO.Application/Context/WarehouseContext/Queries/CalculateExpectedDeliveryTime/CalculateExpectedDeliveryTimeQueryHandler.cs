using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Shipping;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
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
        // 1. Load config for the provider
        var config = await _dbContext.Set<ShippingProviderConfig>()
            .FirstOrDefaultAsync(c => c.ProviderCode == request.ProviderCode, cancellationToken);

        if (config is null)
            return Error.NotFound("ShippingProviderConfig.NotFound", $"Config for provider '{request.ProviderCode}' was not found.");

        // 2. Map to shipping service request
        var timeRequest = new OIO.Application.Abstractions.Shipping.CalculateExpectedDeliveryTimeRequest
        {
            SenderDistrict                  = request.SenderDistrict,
            SenderProvince                  = request.SenderProvince,
            SenderCarrierAddressDataJson    = request.SenderCarrierAddressDataJson,
            RecipientDistrict               = request.RecipientDistrict,
            RecipientProvince               = request.RecipientProvince,
            RecipientCarrierAddressDataJson = request.RecipientCarrierAddressDataJson
        };

        // 3. Delegate to shipping service
        return await _shippingService.CalculateExpectedDeliveryTimeAsync(
            request.ProviderCode,
            timeRequest,
            config,
            cancellationToken);
    }
}

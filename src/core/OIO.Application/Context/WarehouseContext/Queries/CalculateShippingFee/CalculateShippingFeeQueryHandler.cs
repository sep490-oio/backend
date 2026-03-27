using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Shipping;
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
        // 1. Load config for the provider
        var providerCode = ShippingProviderCode.FromId(request.ProviderCode);
        if (providerCode.HasNoValue)
            return Error.NotFound("ShippingProviderConfig.NotFound", $"Config for provider '{request.ProviderCode}' was not found.");

        var config = await _dbContext.Set<ShippingProviderConfig>()
            .FirstOrDefaultAsync(c => c.ProviderCode == providerCode.Value, cancellationToken);

        if (config is null)
            return Error.NotFound("ShippingProviderConfig.NotFound", $"Config for provider '{request.ProviderCode}' was not found.");

        // 2. Map to shipping service request
        var feeRequest = new OIO.Application.Abstractions.Shipping.CalculateFeeRequest
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

        // 3. Delegate to shipping service
        return await _shippingService.CalculateFeeAsync(
            request.ProviderCode,
            feeRequest,
            config,
            cancellationToken);
    }
}

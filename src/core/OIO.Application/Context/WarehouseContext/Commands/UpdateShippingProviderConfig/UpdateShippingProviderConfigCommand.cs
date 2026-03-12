using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.UpdateShippingProviderConfig;

public sealed record UpdateShippingProviderConfigCommand(
    Guid    ConfigId,
    string  DisplayName,
    string  ApiBaseUrl,
    string  PickName,
    string  PickPhone,
    string  PickAddress,
    string  PickWard,
    string  PickDistrict,
    string  PickProvince,
    string? PickCarrierAddressDataJson,
    string? WebhookSecret,
    string? CredentialsJson  // null = don't update credentials
) : ICommand;

internal sealed class UpdateShippingProviderConfigCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<UpdateShippingProviderConfigCommandHandler> logger)
    : ICommandHandler<UpdateShippingProviderConfigCommand>
{
    public async Task<UnitResult<Error>> Handle(
        UpdateShippingProviderConfigCommand request,
        CancellationToken cancellationToken)
    {
        var configId = ShippingProviderConfigId.From(request.ConfigId);
        var now      = clock.UtcNow;

        var config = await db.Set<ShippingProviderConfig>()
            .FirstOrDefaultAsync(c => c.Id == configId, cancellationToken);

        if (config is null)
            return WarehouseErrors.ShippingProvider.NotFound(request.ConfigId.ToString());

        var pickCarrierAddressData = request.PickCarrierAddressDataJson is not null
            ? CarrierAddressData.From(request.PickCarrierAddressDataJson)
            : null;

        config.UpdateDetails(
            displayName:           request.DisplayName,
            apiBaseUrl:            request.ApiBaseUrl,
            pickName:              request.PickName,
            pickPhone:             request.PickPhone,
            pickAddress:           request.PickAddress,
            pickWard:              request.PickWard,
            pickDistrict:          request.PickDistrict,
            pickProvince:          request.PickProvince,
            webhookSecret:         request.WebhookSecret,
            pickCarrierAddressData: pickCarrierAddressData,
            now:                   now);

        if (request.CredentialsJson is not null)
            config.UpdateCredentials(
                ShippingCredentials.From(request.CredentialsJson), now);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "ShippingProviderConfig {ConfigId} updated.", config.Id.Value);

        return UnitResult.Success<Error>();
    }
}
using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Commands.ChooseItemShipping;

public sealed record ChooseItemShippingCommand(
    Guid ItemId,
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
    string? Notes = null) : ICommand<InboundShipmentDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        var check = ChooseItemShippingCommand.Check()
            .WithOwnerName("ChooseItemShipping")
            .Field(ItemId).NotEmptyGuid();

        // Conditional validation:
        // 1. If no external tracking is provided, ProviderCode must be present.
        if (string.IsNullOrWhiteSpace(ExternalTrackingNumber))
        {
            if (string.IsNullOrWhiteSpace(ProviderCode))
            {
                check.State.Fail(WarehouseErrors.ShippingProvider.CodeRequired);
            }
        }
        else
        {
            // 2. If external tracking is provided, ExternalCarrierName must also be present.
            if (string.IsNullOrWhiteSpace(ExternalCarrierName))
            {
                check.State.Fail(WarehouseErrors.InboundShipment.ExternalCarrierNameRequired);
            }
        }

        var result = check.ToViolationsError();

        result.Add(new ItemShippingSelectionRequest(
            SenderName,
            SenderPhone,
            SenderAddress,
            SenderWard,
            SenderDistrict,
            SenderProvince,
            WeightGrams,
            InsuranceValue,
            ProviderCode,
            SenderCarrierAddressDataJson,
            LengthCm,
            WidthCm,
            HeightCm,
            ExternalTrackingNumber,
            ExternalCarrierName,
            Notes).Validate());

        return result;
    }
}

internal sealed class ChooseItemShippingCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    ItemShippingSelectionService itemShippingSelectionService)
    : ICommandHandler<ChooseItemShippingCommand, InboundShipmentDto>
{
    public async Task<Result<InboundShipmentDto, Error>> Handle(
        ChooseItemShippingCommand request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);
        var item = await dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        var selectionRequest = new ItemShippingSelectionRequest(
            request.SenderName,
            request.SenderPhone,
            request.SenderAddress,
            request.SenderWard,
            request.SenderDistrict,
            request.SenderProvince,
            request.WeightGrams,
            request.InsuranceValue,
            request.ProviderCode,
            request.SenderCarrierAddressDataJson,
            request.LengthCm,
            request.WidthCm,
            request.HeightCm,
            request.ExternalTrackingNumber,
            request.ExternalCarrierName,
            request.Notes);

        var shipmentResult = await itemShippingSelectionService.ChooseAsync(
            item,
            currentUser.UserId,
            selectionRequest,
            clock.UtcNow,
            declaredItemPrice: null,
            cancellationToken);
        if (shipmentResult.IsFailure)
            return shipmentResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return shipmentResult.Value.ToDto();
    }
}

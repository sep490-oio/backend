using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.ChooseAuctionShipping;

public sealed record ChooseAuctionShippingCommand(
    Guid AuctionId,
    // Sender info
    string SenderName,
    string SenderPhone,
    string SenderAddress,
    string SenderWard,
    string SenderDistrict,
    string SenderProvince,
    int WeightGrams,
    decimal InsuranceValue,
    // Supported carrier (TH1)
    string? ProviderCode = null,
    string? SenderCarrierAddressDataJson = null,
    int? LengthCm = null,
    int? WidthCm = null,
    int? HeightCm = null,
    // External carrier (TH2) - seller provides tracking number
    string? ExternalTrackingNumber = null,
    string? ExternalCarrierName = null,
    string? Notes = null) : ICommand<InboundShipmentDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        var check = ChooseAuctionShippingCommand.Check()
            .WithOwnerName("ChooseAuctionShipping")
            .Field(AuctionId).NotEmptyGuid()
            .ToViolationsError();

        check.Add(new ItemShippingSelectionRequest(
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

        return check;
    }
}

internal sealed class ChooseAuctionShippingCommandHandler
    : ICommandHandler<ChooseAuctionShippingCommand, InboundShipmentDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ItemShippingSelectionService _itemShippingSelectionService;

    public ChooseAuctionShippingCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        ItemShippingSelectionService itemShippingSelectionService)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _itemShippingSelectionService = itemShippingSelectionService;
    }

    public async Task<Result<InboundShipmentDto, Error>> Handle(
        ChooseAuctionShippingCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var auctionId = AuctionId.From(request.AuctionId);

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: q => q.Include(a => a.Item),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if (auction.Item.SellerId != _currentUser.UserId)
            return AuctionErrors.Auction.OnlyOwnerOfItem;

        if (auction.Status != AuctionStatus.Draft && auction.Status != AuctionStatus.Pending)
            return AuctionErrors.Auction.InvalidState(auction.Status.Id, "choose shipping");

        var serviceRequest = new ItemShippingSelectionRequest(
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

        var shipmentResult = await _itemShippingSelectionService.ChooseAsync(
            auction.Item,
            _currentUser.UserId,
            serviceRequest,
            now,
            auction.Pricing.StartingAmount,
            cancellationToken);
        if (shipmentResult.IsFailure)
            return shipmentResult.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return shipmentResult.Value.ToDto();
    }
}

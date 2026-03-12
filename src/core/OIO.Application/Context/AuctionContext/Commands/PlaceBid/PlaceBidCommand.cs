using System.Net;
using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.PlaceBid;

public sealed record PlaceBidCommand(
    Guid AuctionId,
    decimal Amount,
    string Currency,
    IPAddress? IpAddress) : ICommand<BidDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return PlaceBidCommand
            .Check()
            .WithOwnerName("PlaceBid")
            .Field(AuctionId)
            .NotEmptyGuid()
            .Field(Amount)
            .NotDefault()
            .NonNegative()
            .Field(Currency)
            .NotWhiteSpace()
            .InSet(Domain.Context.Shared.Enums.Currency.All.Select(x => x.Id));
    }
}

internal sealed class PlaceBidCommandHandler
    : ICommandHandler<PlaceBidCommand, BidDto>
{
    private readonly IGrainFactory _grainFactory;
    private readonly ICurrentUser _currentUser;

    public PlaceBidCommandHandler(
        IGrainFactory grainFactory,
        ICurrentUser currentUser)
    {
        _grainFactory = grainFactory;
        _currentUser = currentUser;
    }

    public async Task<Result<BidDto, Error>> Handle(
        PlaceBidCommand request,
        CancellationToken cancellationToken)
    {
        
        var grain = _grainFactory.GetGrain<IAuctionGrain>(request.AuctionId);

        var (_, isFailure, amount, error) = Money.Create(request.Amount, request.Currency); 
        
        if (isFailure)
        {
            return error;
        }
        
        (_, isFailure, var bid, error) = await grain.PlaceBidAsync(
            _currentUser.UserId.Value,
            MoneyGrain.From(amount),
            request.IpAddress,
            cancellationToken);

        if (isFailure)
        {
            return error;
        }

        return new BidDto(
            Id: bid.Id,
            AuctionId: bid.AuctionId,
            BidderId: bid.BidderId,
            Amount: bid.Amount.ToDto(),
            IsAutoBid: bid.IsAutoBid,
            Status: bid.Status,
            CreatedAt: bid.CreatedAt);
    }
}
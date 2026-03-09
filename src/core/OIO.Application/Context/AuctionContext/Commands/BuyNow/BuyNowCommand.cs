using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.BuyNow;

public sealed record BuyNowCommand(Guid AuctionId, IPAddress? IpAddress) : ICommand<BidDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return BuyNowCommand.Check()
            .WithOwnerName("BuyNow")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class BuyNowCommandHandler
    : ICommandHandler<BuyNowCommand, BidDto>
{
    private readonly IGrainFactory _grainFactory;
    private readonly ICurrentUser _currentUser;

    public BuyNowCommandHandler(
        IGrainFactory grainFactory,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _grainFactory = grainFactory;
        _currentUser = currentUser;
    }

    public async Task<Result<BidDto, Error>> Handle(
        BuyNowCommand request,
        CancellationToken cancellationToken)
    {

        var grain = _grainFactory.GetGrain<IAuctionGrain>(request.AuctionId);
        
        var (_, isFailure, bid, error)  = await grain.ExecuteBuyNowAsync(
            _currentUser.UserId.Value,
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
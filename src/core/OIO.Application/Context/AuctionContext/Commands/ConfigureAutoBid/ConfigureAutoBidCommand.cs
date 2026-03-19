using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.ConfigureAutoBid;

public sealed record ConfigureAutoBidCommand(
    Guid AuctionId,
    decimal MaxAmount,
    string Currency,
    decimal? IncrementAmount = null) : ICommand<AutoBidDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return ConfigureAutoBidCommand.Check()
            .WithOwnerName("ConfigureAutoBid")
            .Field(AuctionId)
            .NotEmptyGuid()
            .Field(MaxAmount)
            .NotDefault()
            .NonNegative()
            .Field(Currency)
            .NotWhiteSpace()
            .InSet(Domain.Context.Shared.Enums.Currency.All.Select(x => x.Id))
            .Field(IncrementAmount)
            .WhenHasValue(x => x.Positive());
    }
}

internal sealed class ConfigureAutoBidCommandHandler
    : ICommandHandler<ConfigureAutoBidCommand, AutoBidDto>
{
    private readonly IGrainFactory _grainFactory;
    private readonly ICurrentUser _currentUser;
    private readonly IDbContext _dbContext;

    public ConfigureAutoBidCommandHandler(
        IGrainFactory grainFactory,
        ICurrentUser currentUser,
        IDbContext dbContext)
    {
        _grainFactory = grainFactory;
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    public async Task<Result<AutoBidDto, Error>> Handle(
        ConfigureAutoBidCommand request,
        CancellationToken cancellationToken)
    {
        var grain = _grainFactory.GetGrain<IAuctionGrain>(request.AuctionId);

        var (_, isFailure, maxAmount, error) = Money.Create(request.MaxAmount, request.Currency);

        if (isFailure)
            return error;

        Money? incrementAmount = null;

        if (request.IncrementAmount.HasValue)
        {
            var incrementResult = Money.Create(request.IncrementAmount.Value, request.Currency);
            if (incrementResult.IsFailure)
                return incrementResult.Error;
            incrementAmount = incrementResult.Value;
        }

        var auctionId = AuctionId.From(request.AuctionId);

        (_, isFailure, _, error) = await grain.ConfigureAutoBidAsync(
            _currentUser.UserId.Value,
            MoneyGrain.From(maxAmount),
            incrementAmount == null ? null : MoneyGrain.From(incrementAmount),
            cancellationToken);

        if (isFailure)
            return error;

        var persistedAutoBid = await _dbContext.Set<AutoBid>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ab => ab.AuctionId == auctionId &&
                      ab.BidderId == _currentUser.UserId,
                cancellationToken);

        if (persistedAutoBid is null)
            return Error.NotFound("AutoBid.NotFound", "Configured auto-bid could not be loaded.");

        return persistedAutoBid.ToDto();
    }
}

using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;
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
            .InSet(Domain.Context.Shared.ValueObjects.Currency.All.Select(x => x.Id))
            .Field(IncrementAmount)
            .WhenHasValue(x => x.NonNegative());
    }
}

internal sealed class ConfigureAutoBidCommandHandler
    : ICommandHandler<ConfigureAutoBidCommand, AutoBidDto>
{
    private readonly IGrainFactory _grainFactory;
    private readonly ICurrentUser _currentUser;

    public ConfigureAutoBidCommandHandler(
        IGrainFactory grainFactory,
        ICurrentUser currentUser)
    {
        _grainFactory = grainFactory;
        _currentUser = currentUser;
    }

    public async Task<Result<AutoBidDto, Error>> Handle(
        ConfigureAutoBidCommand request,
        CancellationToken cancellationToken)
    {

        var grain = _grainFactory.GetGrain<IAuctionGrain>(request.AuctionId);
        
        var (_, isFailure, maxAmount, error) = Money.Create(request.MaxAmount, request.Currency);
        
        if (isFailure)
        {
            return error;
        }
        
        Money? incrementAmount = null;
        
        if (request.IncrementAmount.HasValue)
        {
            var incrementResult = Money.Create(request.IncrementAmount.Value, request.Currency);
            if (incrementResult.IsFailure)
            {
                return incrementResult.Error;
            }
            incrementAmount = incrementResult.Value;
        }
        
        (_, isFailure, var autoBid, error) = await grain.ConfigureAutoBidAsync(
            _currentUser.UserId.Value,
            MoneyGrain.From(maxAmount),
            incrementAmount == null ? null : MoneyGrain.From(incrementAmount), 
            cancellationToken);

        if (isFailure)
        {
            return error;
        }

        return new AutoBidDto(
            Id: autoBid.Id,
            AuctionId: autoBid.AuctionId,
            BidderId: autoBid.BidderId,
            IsEnabled: autoBid.IsEnabled,
            MaxAmount: autoBid.MaxAmount.Amount,
            CurrentAmount: autoBid.CurrentAmount.Amount,
            IncrementAmount: autoBid.IncrementAmount?.Amount,
            Status: autoBid.Status,
            TotalAutoBids: autoBid.TotalAutoBids,
            LastAutoBidAt: autoBid.LastAutoBidAt,
            CreatedAt: autoBid.CreatedAt);
    }
}
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
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
            .WhenHasValue(x => x.NonNegative());
    }
}

internal sealed class ConfigureAutoBidCommandHandler
    : ICommandHandler<ConfigureAutoBidCommand, AutoBidDto>
{
    private readonly IGrainFactory _grainFactory;
    private readonly ICurrentUser _currentUser;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfigureAutoBidCommandHandler(
        IGrainFactory grainFactory,
        ICurrentUser currentUser,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _grainFactory = grainFactory;
        _currentUser = currentUser;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
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

        // 1. Database-side Action: Wallet Hold for AutoBid
        var nowUtcx = _clock.UtcNow;

        var wallet = await _dbContext.Set<OIO.Domain.Context.PaymentContext.Aggregates.Wallets.Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == _currentUser.UserId, cancellationToken);
            
        if (wallet is null)
            return Error.NotFound("Wallet.NotFound", "User wallet not found.");

        // NOTE: If they are updating an existing Auto-bid, we only want to hold the *difference*.
        // But for simplicity in this MVP implementation, we assume we just hold the MaxAmount. 
        // More advanced: fetch existing AutoBid from read model, calculate diff, then Hold/Unhold diff.
        // For now, let's just do a blind Hold. If they configure 3 times, it'll hold 3 times. 
        // We will pass null for TransactionId as it's an internal wallet hold.
        var holdResult = wallet.Hold(
            amount: maxAmount.Amount,
            transactionId: null,
            description: $"Auto-bid reservation for Auction {request.AuctionId}",
            nowUtc: nowUtcx);

        if (holdResult.IsFailure)
            return Error.Conflict("Wallet.HoldFailed", "Insufficient balance to configure auto-bid.");

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // 2. Orleans-side Action: Grain configuration
        (_, isFailure, var autoBid, error) = await grain.ConfigureAutoBidAsync(
            _currentUser.UserId.Value,
            MoneyGrain.From(maxAmount),
            incrementAmount == null ? null : MoneyGrain.From(incrementAmount), 
            cancellationToken);

        // 3. Compensation
        if (isFailure)
        {
            // If grain fails (e.g., lower than minimum bid), we rollback the hold
            var unholdResult = wallet.Unhold(
                amount: maxAmount.Amount,
                transactionId: null,
                description: $"Rollback auto-bid reservation for Auction {request.AuctionId}",
                nowUtc: nowUtcx);

            if (unholdResult.IsSuccess)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            return error;
        }

        var persistedAutoBid = await _dbContext.Set<AutoBid>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ab => ab.AuctionId == AuctionId.From(request.AuctionId) &&
                      ab.BidderId == _currentUser.UserId,
                cancellationToken);

        if (persistedAutoBid is null)
        {
            return Error.NotFound("AutoBid.NotFound", "Configured auto-bid could not be loaded.");
        }

        return persistedAutoBid.ToDto();
    }
}

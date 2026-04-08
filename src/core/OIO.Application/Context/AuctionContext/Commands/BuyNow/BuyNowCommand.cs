using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.OrderContext.Factories;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using AuctionId = OIO.Domain.Context.AuctionContext.ValueObjects.Ids.AuctionId;

namespace OIO.Application.Context.AuctionContext.Commands.BuyNow;

public sealed record BuyNowCommand(Guid AuctionId, IPAddress? IpAddress) : ICommand<BuyNowReservationDto>, IHasValidate
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
    : ICommandHandler<BuyNowCommand, BuyNowReservationDto>
{
    private static readonly TimeSpan ReservationWindow = TimeSpan.FromMinutes(15);

    private readonly IGrainFactory _grainFactory;
    private readonly ICurrentUser _currentUser;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<BuyNowCommandHandler> _logger;

    public BuyNowCommandHandler(
        IGrainFactory grainFactory,
        ICurrentUser currentUser,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<BuyNowCommandHandler> logger)
    {
        _grainFactory = grainFactory;
        _currentUser = currentUser;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<BuyNowReservationDto, Error>> Handle(
        BuyNowCommand request,
        CancellationToken cancellationToken)
    {
        var grain = _grainFactory.GetGrain<IAuctionGrain>(request.AuctionId);

        var (_, isFailure, reservation, error) = await grain.InitiateBuyNowReservationAsync(
            _currentUser.UserId.Value,
            ReservationWindow,
            cancellationToken);

        if (isFailure)
            return error;

        // Defensive: a reservation returned with an existing OrderId means we
        // are observing an idempotent re-issue. Load and return that order.
        if (reservation.OrderId.HasValue)
        {
            var existingOrder = await _dbContext.GetByIdAsync<Order, OrderId>(
                id: OrderId.From(reservation.OrderId.Value),
                cancellationToken: cancellationToken);

            if (existingOrder is not null)
            {
                if (existingOrder.BuyerId != _currentUser.UserId)
                {
                    _logger.LogWarning(
                        "Buy-now idempotent re-issue blocked: order {OrderId} buyer {OrderBuyerId} does not match current user {CurrentUserId}",
                        existingOrder.Id.Value,
                        existingOrder.BuyerId.Value,
                        _currentUser.UserId.Value);
                    return Error.Forbidden(
                        "AuctionBuyNow.Forbidden",
                        "You are not authorized to access this buy-now reservation.");
                }

                return BuildDto(reservation.Id, existingOrder.Id.Value, reservation, existingOrder.PaymentDueAt);
            }
        }

        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.Set<Auction>()
            .Include(x => x.Item)
            .Include(x => x.BuyNowReservations)
            .FirstOrDefaultAsync(x => x.Id == auctionId, cancellationToken);

        if (auction is null)
            return Error.NotFound("Auction.NotFound", "Auction not found for buy-now order creation.");

        var buyer = await _dbContext.Set<User>()
            .Include(x => x.Profile)
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(x => x.Id == _currentUser.UserId, cancellationToken);

        if (buyer is null)
            return Error.NotFound("User.NotFound", "Buyer not found for buy-now order creation.");

        var reservationDomain = auction.BuyNowReservations
            .FirstOrDefault(r => r.Id.Value == reservation.Id);

        if (reservationDomain is null)
        {
            _logger.LogError(
                "Buy-now reservation {ReservationId} created via grain not found in DB projection of auction {AuctionId}",
                reservation.Id,
                request.AuctionId);
            return Error.NotFound("AuctionBuyNowReservation.NotFound", "Buy-now reservation not found after creation.");
        }

        var nowUtc = _clock.UtcNow;

        var orderResult = BuyNowOrderFactory.Create(
            auction,
            buyer,
            reservationDomain,
            nowUtc,
            notes: "Created from buy-now reservation (order-first flow).");

        if (orderResult.IsFailure)
            return orderResult.Error;

        var order = orderResult.Value;
        _dbContext.Insert(order);

        var linkResult = auction.LinkBuyNowReservationOrder(reservationDomain.Id, order.Id, nowUtc);
        if (linkResult.IsFailure)
            return linkResult.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BuildDto(reservation.Id, order.Id.Value, reservation, expiresAtOverride: null);
    }

    private static BuyNowReservationDto BuildDto(
        Guid reservationId,
        Guid orderId,
        Domain.Context.AuctionContext.Grains.GrainModels.AuctionBuyNowReservationGrain reservation,
        DateTime? expiresAtOverride)
    {
        var buyNowPriceDto = reservation.BuyNowPrice.ToDto();
        var depositAppliedDto = reservation.DepositAppliedAmount.ToDto();
        var amountDueDto = new MoneyDto(
            buyNowPriceDto.Amount - depositAppliedDto.Amount,
            buyNowPriceDto.Currency,
            buyNowPriceDto.Symbol);

        return new BuyNowReservationDto(
            ReservationId: reservationId,
            OrderId: orderId,
            ExpiresAt: expiresAtOverride ?? reservation.ExpiresAt,
            BuyNowPrice: buyNowPriceDto,
            DepositAppliedAmount: depositAppliedDto,
            AmountDue: amountDueDto);
    }
}

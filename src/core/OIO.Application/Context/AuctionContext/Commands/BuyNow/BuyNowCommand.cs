using System.Net;
using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Payment;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.PaymentContext.Commands.CreateVnPayPaymentUrl;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.BuyNow;

public sealed record BuyNowCommand(Guid AuctionId, IPAddress? IpAddress) : ICommand<BuyNowCheckoutDto>, IHasValidate
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
    : ICommandHandler<BuyNowCommand, BuyNowCheckoutDto>
{
    private static readonly TimeSpan ReservationWindow = TimeSpan.FromMinutes(15);

    private readonly IGrainFactory _grainFactory;
    private readonly ICurrentUser _currentUser;
    private readonly MediatR.ISender _sender;

    public BuyNowCommandHandler(
        IGrainFactory grainFactory,
        ICurrentUser currentUser,
        MediatR.ISender sender)
    {
        _grainFactory = grainFactory;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<Result<BuyNowCheckoutDto, Error>> Handle(
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

        var createPaymentUrl = await _sender.Send(
            new CreateVnPayPaymentUrlCommand(
                Amount: reservation.GatewayAmountDue.Amount,
                Currency: reservation.GatewayAmountDue.Currency,
                Purpose: PaymentPurpose.AuctionBuyNow.Id,
                IpAddress: request.IpAddress ?? IPAddress.Loopback,
                Description: $"AuctionBuyNow - Auction #{request.AuctionId}",
                AuctionId: request.AuctionId,
                BuyNowReservationId: reservation.Id),
            cancellationToken);

        if (createPaymentUrl.IsFailure)
        {
            await grain.FailBuyNowReservationAsync(
                reservation.Id,
                "payment_url_creation_failed",
                cancellationToken);

            return createPaymentUrl.Error;
        }

        var attachResult = await grain.AttachBuyNowPaymentAsync(
            reservation.Id,
            createPaymentUrl.Value.TransactionId,
            cancellationToken);

        if (attachResult.IsFailure)
        {
            await grain.FailBuyNowReservationAsync(
                reservation.Id,
                "payment_transaction_attach_failed",
                cancellationToken);

            return attachResult.Error;
        }

        return new BuyNowCheckoutDto(
            ReservationId: reservation.Id,
            PaymentUrl: createPaymentUrl.Value.PaymentUrl,
            ExpiresAt: reservation.ExpiresAt,
            BuyNowPrice: reservation.BuyNowPrice.ToDto(),
            DepositAppliedAmount: reservation.DepositAppliedAmount.ToDto(),
            AmountDue: reservation.GatewayAmountDue.ToDto());
    }
}

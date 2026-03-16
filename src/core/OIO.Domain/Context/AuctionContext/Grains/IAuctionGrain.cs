using System.Net;
using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Grains.GrainModels;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Grains;

/// <summary>
/// Orleans grain interface for auction operations.
/// Grain ID = AuctionId (Guid).
/// Single-threaded per auction → no race conditions.
/// </summary>
public interface IAuctionGrain : IGrainWithGuidKey
{
    Task<Result<BidGrain, Error>> PlaceBidAsync(
        Guid bidderId,
        MoneyGrain amount,
        IPAddress? ipAddress,
        CancellationToken cancellationToken = default);

    Task<Result<AuctionBuyNowReservationGrain, Error>> InitiateBuyNowReservationAsync(
        Guid bidderId,
        TimeSpan reservationWindow,
        CancellationToken cancellationToken = default);

    Task<Result<AuctionBuyNowReservationGrain, Error>> AttachBuyNowPaymentAsync(
        Guid reservationId,
        Guid paymentTransactionId,
        CancellationToken cancellationToken = default);

    Task<UnitResult<Error>> FailBuyNowReservationAsync(
        Guid reservationId,
        string reason,
        CancellationToken cancellationToken = default);
    
    Task<Result<AutoBidGrain, Error>> ConfigureAutoBidAsync(
        Guid bidderId,
        MoneyGrain maxAmount,
        MoneyGrain? incrementAmount,
        CancellationToken cancellationToken = default);
    Task<Result<AuctionSnapshotGrain, Error>> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

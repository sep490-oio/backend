using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Services;

public interface IBiddingDomainService
{
    // Xử lý luồng đặt giá phức tạp liên quan đến nhiều thực thể
    Task<UnitResult<Error>> ExecutePlaceBidAsync(
        Auction auction, 
        Guid bidderId, 
        Money amount, 
        DateTime nowUtc);
}
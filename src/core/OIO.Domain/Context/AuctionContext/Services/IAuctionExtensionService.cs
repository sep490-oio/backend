using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

namespace OIO.Domain.Context.AuctionContext.Services;

public interface IAuctionExtensionService
{
    /// <summary>
    /// Tính toán thời gian kết thúc mới nếu có người bid vào phút chót (Sniper Protection)
    /// </summary>
    DateTime CalculateExtendedEndTime(Auction auction, DateTime bidTime);

    /// <summary>
    /// Tính toán phí sàn hoặc phí dịch vụ dựa trên giá trị đấu giá
    /// </summary>
    decimal CalculateAuctionFees(decimal finalPrice);
}
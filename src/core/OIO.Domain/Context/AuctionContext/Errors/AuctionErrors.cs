using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Errors;

public static class AuctionErrors
{
    public static class Auction
    {
        // NotFound nhận 2 tham số: code, description
        public static Error NotFound(Guid id) => 
            Error.NotFound("Auction.NotFound", $"Không tìm thấy cuộc đấu giá với Id: {id}");

        // Validation nhận 3 tham số: propertyName, code, description
        public static readonly Error Deleted = Error.Validation("Auction", "Auction.Deleted", "Cuộc đấu giá này đã bị xóa.");
        
        // Forbidden nhận 2 tham số: code, description
        public static readonly Error SelfBid = Error.Forbidden("Auction.SelfBid", "Người bán không thể tự đặt giá cho sản phẩm của mình.");
    }

    public static class Bid
    {
        // Validation nhận 3 tham số
        public static Error TooLow(decimal minAmount) => 
            Error.Validation("Amount", "Bid.TooLow", $"Giá đặt phải tối thiểu là {minAmount}");

        public static readonly Error DepositRequired = Error.Forbidden("Bid.DepositRequired", "Bạn cần đặt cọc để tham gia đấu giá này.");
    }
    public static class Item
    {
        public static readonly Error MaxImagesReached = Error.Validation("Images", "Item.MaxImagesReached", "Đã đạt giới hạn 10 hình ảnh cho một sản phẩm.");
        
        public static Error NotFound(Guid id) => 
            Error.NotFound("Item.NotFound", $"Không tìm thấy sản phẩm với Id: {id}");
    }
}
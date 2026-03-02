namespace OIO.Domain.Context.AuctionContext.Enums;

public enum BidStatus
{
    Active = 1,     // Giá đặt hợp lệ
    Outbid = 2,     // Đã bị người khác đặt giá cao hơn
    Winner = 3,     // Là giá thắng cuộc cuối cùng
    Cancelled = 4   // Giá bị hủy (do vi phạm hoặc yêu cầu đặc biệt)
}
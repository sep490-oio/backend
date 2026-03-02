namespace OIO.Domain.Context.AuctionContext.Enums;

public enum AutoBidStatus
{
    Active = 1,     // Đang hoạt động
    Outpaced = 2,   // Đã bị vượt qua (giá thị trường cao hơn MaxAmount)
    Finished = 3,   // Đã kết thúc (thắng cuộc)
    Disabled = 4    // Người dùng chủ động tắt
}
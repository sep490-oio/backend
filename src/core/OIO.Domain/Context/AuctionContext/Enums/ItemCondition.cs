namespace OIO.Domain.Context.AuctionContext.Enums;

public enum ItemCondition
{
    New = 1,            // Mới 100%
    LikeNew = 2,        // Như mới (99%)
    UsedGood = 3,       // Đã qua sử dụng (Tốt)
    UsedFair = 4,       // Đã qua sử dụng (Trung bình)
    Refurbished = 5     // Hàng tân trang
}
namespace OIO.Domain.Context.AuctionContext.Enums;

public enum ItemStatus
{
    Pending = 1,        // Chờ duyệt
    Approved = 2,       // Đã duyệt (Sẵn sàng đấu giá)
    InAuction = 3,      // Đang trong phiên đấu giá
    Sold = 4,           // Đã bán
    Rejected = 5,       // Bị từ chối
    Withdrawn = 6       // Người bán rút lại
}
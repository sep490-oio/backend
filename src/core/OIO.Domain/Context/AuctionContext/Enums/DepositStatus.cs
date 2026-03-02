namespace OIO.Domain.Context.AuctionContext.Enums;

public enum DepositStatus
{
    Pending = 1,    // Đang chờ thanh toán cọc
    Confirmed = 2,  // Đã xác nhận cọc (có thể bắt đầu bid)
    Refunded = 3,   // Đã hoàn cọc (khi đấu giá kết thúc mà không thắng)
    Forfeited = 4,  // Bị tịch thu (khi thắng nhưng không thanh toán)
    Cancelled = 5   // Đã hủy
}
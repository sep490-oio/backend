namespace OIO.Application.Abstractions.Mail;

public interface IUserMailNotifier
{
    Task SendWelcomeVerifyAsync(
        string toEmail,
        string userName,
        string token,
        string userId,
        DateTime tokenExpiry,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(
        string toEmail, 
        string userName,
        string token,
        DateTime tokenExpiry,
        CancellationToken cancellationToken = default);

    Task SendResendVerifyAsync(
        string toEmail,
        string userName,
        string token,
        string userId,
        DateTime tokenExpiry,
        CancellationToken cancellationToken = default);

    // ==================== Security ====================
    Task SendPasswordChangedAlertAsync(
        string toEmail, 
        string userName,
        CancellationToken cancellationToken = default);
    
    // ==================== Auction Emails ====================

    Task SendOutbidAsync(
        string toEmail, 
        string userName,
        string auctionTitle,
        decimal newPrice,
        string auctionUrl,
        string currency,
        CancellationToken cancellationToken = default);

    Task SendAuctionWonAsync(
        string toEmail, 
        string userName,
        string auctionId,
        string auctionTitle,
        decimal finalPrice,
        string currency,
        string paymentUrl,
        CancellationToken cancellationToken = default);

    Task SendAuctionSoldAsync(
        string toEmail,
        string userName,
        string auctionId,
        string auctionTitle,
        string winnerName,
        decimal finalPrice,
        string currency,
        CancellationToken cancellationToken = default);

    Task SendAuctionFailedAsync(
        string toEmail,
        string userName,
        string auctionId,
        string auctionTitle,
        string reason,
        decimal finalPrice,
        int totalBids,
        string currency,
        CancellationToken cancellationToken = default);

    Task SendAuctionEndedWatcherAsync(
        string toEmail, 
        string userName,
        string auctionId,
        string auctionTitle,
        decimal finalPrice,
        string currency,
        bool hasWinner,
        CancellationToken cancellationToken = default);

    Task SendAuctionCancelledAsync(
        string toEmail,
        string userName,
        string auctionId,
        string auctionTitle,
        string reason,
        CancellationToken cancellationToken = default);

}
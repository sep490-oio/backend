namespace OIO.Infrastructure.Mail.RazorEmails.Rendering.ViewModels;

public class PhoneVerificationMailViewModel
{
    public required string UserName { get; set; }
    public required string OtpCode { get; set; }
    public required string ExpiredAt { get; set; }
}

public sealed class ResendVerifyMailViewModel
{
    public required string UserName { get; set; }
    public required string VerifyUrl { get; set; }
    public required string ExpiredAt { get; set; }
}
    
public sealed class OutbidMailViewModel
{
    public required string UserName { get; set; }
    public required string AuctionTitle { get; set; }
    public required decimal NewHighestBid { get; set; }
    public required string Currency { get; set; }
    public required string AuctionUrl { get; set; } 

}
    
public sealed class AuctionWonMailViewModel
{
    public required string UserName { get; set; }
    public required string AuctionId { get; set; }
    public required string AuctionTitle { get; set; }
    public required decimal FinalPrice { get; set; }
    public required string Currency { get; set; }
    public required string PaymentUrl { get; set; }
    public required int PaymentDeadlineHours { get; set; }
}

public sealed class AuctionSoldMailViewModel
{
    public required string UserName  { get; set; }
    public required string AuctionId  { get; set; }
    public required string AuctionTitle  { get; set; }
    public required string WinnerName  { get; set; }
    public required decimal FinalPrice  { get; set; }
    public required string Currency  { get; set; }
}

public sealed class AuctionFailedMailViewModel
{
    public required string UserName { get; set; }
    public required string AuctionId { get; set; }
    public required string AuctionTitle { get; set; }
    public required string Reason { get; set; }
    public required decimal FinalPrice { get; set; }
    public required string Currency { get; set; }
    public required int TotalBids { get; set; }
    public required string RelistUrl { get; set; }
}


public sealed class AuctionEndedWatcherMailViewModel
{
    public required string UserName { get; set; }
    public required string AuctionId { get; set; }
    public required string AuctionTitle { get; set; }
    public required decimal FinalPrice { get; set; }
    public required string Currency { get; set; }
    public required bool HasWinner { get; set; }
}
    
public sealed class AuctionCancelledMailViewModel
{
    public required string UserName { get; set; }
    public required string AuctionId { get; set; }
    public required string AuctionTitle { get; set; }
    public required string Reason { get; set; } }
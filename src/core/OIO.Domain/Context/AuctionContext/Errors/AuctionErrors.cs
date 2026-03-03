using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Errors;

public static class AuctionErrors
{
    public static class Auction
    {
        public static Error NotFound(Guid id) => 
            Error.NotFound("Auction.NotFound", $"Auction with Id '{id}' was not found.");
        
        public static readonly Error SelfBid = 
            Error.Forbidden("Auction.SelfBid", "Sellers are not allowed to bid on their own auctions.");

        public static readonly Error InvalidStatus = 
            Error.Conflict("Auction.InvalidStatus", "The auction is not in an active state.");

        public static readonly Error Expired = 
            Error.Validation("Period", "Auction.Expired", "The auction period has already ended.");

        public static readonly Error InvalidBuyNow = 
            Error.Validation("BuyNowPrice", "Auction.InvalidBuyNow", 
                "Buy now price must be greater than starting price.");

        public static readonly Error InvalidIncrement = 
            Error.Validation("Increment", "Auction.InvalidIncrement", 
                "Bid increment must be a positive value.");

        public static readonly Error InvalidPeriod = 
            Error.Validation("EndTime", "Auction.InvalidPeriod", 
                "End time must be later than start time.");

        public static readonly Error InvalidReserve = 
            Error.Validation("ReservePrice", "Auction.InvalidReserve", 
                "Reserve price must be greater than or equal to starting price.");
        
        public static readonly Error InvalidStartingPrice = 
            Error.Validation("StartingPrice", "Auction.InvalidStartingPrice", 
                "Starting price must be a non-negative value.");
            
        public static readonly Error Deleted = 
            Error.Conflict("Auction.Deleted", "This auction has been deleted and cannot be modified.");

        public static Error InvalidInput(string details) => 
            Error.Validation("Input", "Auction.InvalidInput", $"Invalid auction input: {details}");

        public static readonly Error CannotCancelActive = 
            Error.Conflict("Auction.CannotCancelActive", 
                "Cannot cancel an auction that has already ended or been sold.");

        public static readonly Error NotDraft = 
            Error.Conflict("Auction.NotDraft", 
                "Only draft auctions can be activated.");

        public static readonly Error AlreadyActive = 
            Error.Conflict("Auction.AlreadyActive", 
                "Auction is already active and cannot be activated again.");
    }

    public static class Bid
    {
        public static Error TooLow(decimal minAmount) => 
            Error.Validation("Amount", "Bid.TooLow", 
                $"The bid amount must be at least {minAmount}.");

        public static readonly Error DepositRequired = 
            Error.Forbidden("Bid.DepositRequired", 
                "A deposit is required to participate in this auction.");

        public static Error NotFound(Guid id) => 
            Error.NotFound("Bid.NotFound", $"Bid with Id '{id}' was not found.");

        public static readonly Error BidAlreadyOutbid = 
            Error.Conflict("Bid.Outbid", "This bid has already been outbid.");
    }

    public static class AutoBid
    {
        public static Error NotFound(Guid id) => 
            Error.NotFound("AutoBid.NotFound", $"Auto bid with Id '{id}' was not found.");

        public static Error ExceedsMaxAmount(decimal maxAmount) => 
            Error.Validation("CurrentAmount", "AutoBid.ExceedsMaxAmount", 
                $"The current amount cannot exceed the maximum amount of {maxAmount}.");

        public static readonly Error IsDisabled = 
            Error.Conflict("AutoBid.IsDisabled", 
                "This auto bid is currently disabled and cannot be updated.");

        public static readonly Error CannotPause = 
            Error.Validation("Status", "AutoBid.CannotPause", 
                "This auto bid cannot be paused in its current state.");

        public static readonly Error CannotResume = 
            Error.Validation("Status", "AutoBid.CannotResume", 
                "This auto bid cannot be resumed from its current state.");

        public static readonly Error CannotModifyFinalStatus = 
            Error.Conflict("AutoBid.CannotModifyFinalStatus", 
                "Cannot modify auto bid after it has been won or outbid.");

        public static Error InvalidInput(string details) => 
            Error.Validation("Input", "AutoBid.InvalidInput", 
                $"Invalid auto bid configuration: {details}");

        public static readonly Error AlreadyExists = 
            Error.Conflict("AutoBid.AlreadyExists", 
                "An auto bid already exists for this bidder on this auction.");

        public static readonly Error InvalidMaxAmount = 
            Error.Validation("MaxAmount", "AutoBid.InvalidMaxAmount", 
                "Maximum amount must be greater than zero.");

        public static readonly Error InvalidCurrentAmount = 
            Error.Validation("CurrentAmount", "AutoBid.InvalidCurrentAmount", 
                "Current amount must not exceed maximum amount.");
    }

    public static class Deposit
    {
        public static Error InvalidInput(string details) => 
            Error.Validation("Input", "Deposit.InvalidInput", $"Invalid Deposit input: {details}");

        public static Error NotFound(Guid id) => 
            Error.NotFound("Deposit.NotFound", $"Deposit with Id '{id}' was not found.");

        public static readonly Error InsufficientFunds = 
            Error.Validation("Amount", "Deposit.InsufficientFunds", 
                "The deposit amount exceeds available funds.");

        public static readonly Error AlreadyExists = 
            Error.Conflict("Deposit.AlreadyExists", 
                "A deposit already exists for this user on this auction.");

        public static readonly Error InvalidAmount = 
            Error.Validation("Amount", "Deposit.InvalidAmount", 
                "Deposit amount must be greater than zero.");

        public static readonly Error CannotRelease = 
            Error.Conflict("Deposit.CannotRelease", 
                "This deposit is in a state where it cannot be released.");

        public static readonly Error CannotReturn = 
            Error.Conflict("Deposit.CannotReturn", 
                "Only held deposits can be returned.");

        public static readonly Error CannotForfeit = 
            Error.Conflict("Deposit.CannotForfeit", 
                "Only held deposits can be forfeited.");

        public static readonly Error CannotConvert = 
            Error.Conflict("Deposit.CannotConvert", 
                "Only held deposits can be converted to payment.");
    }

    public static class Watcher
    {
        public static Error NotFound(Guid auctionId, Guid userId) => 
            Error.NotFound("Watcher.NotFound", 
                $"No watcher record found for auction '{auctionId}' and user '{userId}'.");

        public static readonly Error AlreadyWatching = 
            Error.Conflict("Watcher.AlreadyWatching", 
                "User is already watching this auction.");

        public static readonly Error CannotWatchOwnAuction = 
            Error.Forbidden("Watcher.CannotWatchOwnAuction", 
                "Sellers cannot watch their own auctions.");
    }

    public static class Item
    {
        public static Error NotFound(Guid id) => 
            Error.NotFound("Item.NotFound", $"Item with Id '{id}' was not found.");

        public static readonly Error MaxImagesReached = 
            Error.Validation("Images", "Item.MaxImagesReached", 
                "Maximum of 10 images reached for this item.");

        public static readonly Error InvalidCondition = 
            Error.Validation("Condition", "Item.InvalidCondition", 
                "Item condition must be one of the allowed values.");

        public static readonly Error CannotAuction = 
            Error.Conflict("Item.CannotAuction", 
                "This item is not in a state where it can be auctioned.");
    }

    public static class PriceHistory
    {
        public static Error NotFound(Guid id) => 
            Error.NotFound("PriceHistory.NotFound", 
                $"Price history record with Id '{id}' was not found.");

        public static readonly Error AuctionIdEmpty = 
            Error.Validation("AuctionId", "PriceHistory.AuctionIdEmpty", 
                "AuctionId cannot be empty.");

        public static readonly Error NegativePrice = 
            Error.Validation("Price", "PriceHistory.NegativePrice", 
                "Price cannot be negative.");

        public static readonly Error InvalidRecordedAt = 
            Error.Validation("RecordedAt", "PriceHistory.InvalidRecordedAt", 
                "RecordedAt cannot be default.");

        public static readonly Error NonUtcDateTime = 
            Error.Validation("RecordedAt", "PriceHistory.NonUtcDateTime", 
                "RecordedAt must be UTC.");

        public static Error InvalidInput(string details) =>
            Error.Validation("Input", "PriceHistory.InvalidInput", $"Invalid price history: {details}");
    }
}
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Errors;

public static class AuctionErrors
{
    public static class Auction
    {
        public static Error NotFound(Guid id) => 
            Error.NotFound("Auction.NotFound", $"Auction with Id {id} was not found.");
        
        public static readonly Error SelfBid = 
            Error.Forbidden("Auction.SelfBid", "Sellers are not allowed to bid on their own items.");

        public static readonly Error InvalidStatus = 
            Error.Conflict("Auction.InvalidStatus", "The auction is not in an active state.");

        public static readonly Error Expired = 
// Property, Code, Description
            Error.Validation("Time", "Auction.Expired", "The auction period has already ended.");

        public static readonly Error InvalidBuyNow = 
            Error.Validation("BuyNowPrice", "Auction.InvalidBuyNow", "Buy now price must be greater than starting price.");

        public static readonly Error InvalidIncrement = 
            Error.Validation("Increment", "Auction.InvalidIncrement", "Bid increment must be a positive value.");

        public static readonly Error InvalidPeriod = 
            Error.Validation("EndTime", "Auction.InvalidPeriod", "End time must be later than start time.");

        public static readonly Error InvalidReserve = 
            Error.Validation("ReservePrice", "Auction.InvalidReserve", "Reserve price must be greater than or equal to starting price.");
            
        public static readonly Error Deleted = 
            Error.Validation("Auction", "Auction.Deleted", "This auction has been deleted.");
                  
        public static readonly Error InvalidStartingPrice = 
            Error.Validation("StartingPrice", "Auction.InvalidStartingPrice", "Starting price must be a non-negative value.");
    }
    

    public static class Bid
    {
        public static Error TooLow(decimal minAmount) => 
            Error.Validation("Amount", "Bid.TooLow", $"The bid amount must be at least {minAmount}.");

        public static readonly Error DepositRequired = 
            Error.Forbidden("Bid.DepositRequired", "A deposit is required to participate in this auction.");
    }

    public static class Item
    {
        public static readonly Error MaxImagesReached = 
            Error.Validation("Images", "Item.MaxImagesReached", "Maximum of 10 images reached for this item.");
        
        public static Error NotFound(Guid id) => 
            Error.NotFound("Item.NotFound", $"Item with Id {id} was not found.");
    }
}
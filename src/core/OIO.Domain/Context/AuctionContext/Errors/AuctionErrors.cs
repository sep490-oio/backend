using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using CategoryId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.CategoryId;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;
using ItemMediaId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemMediaId;
using ItemQuestionId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemQuestionId;

namespace OIO.Domain.Context.AuctionContext.Errors;

public static class AuctionErrors
{
    public static class Auction
    {
        public static Error NotFound(AuctionId id) => 
            Error.NotFound("Auction.NotFound", $"Auction with Id '{id}' was not found.");
        
        public static readonly Error SelfBid = 
            Error.Forbidden("Auction.SelfBid", "Sellers are not allowed to bid on their own auctions.");

        public static readonly Error InvalidStatus = 
            Error.Conflict("Auction.InvalidStatus", "The auction is not in an active state.");
        
        public static Error InvalidState(string currentState, string attemptedAction) => 
            Error.Conflict(
                "Auction.InvalidState", 
                $"Cannot perform '{attemptedAction}' when auction status is '{currentState}'.");

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

        public static readonly Error NotSupportBuyNow = Error.Conflict(
            "Auction.NotSupportBuyNow",
            "This auction does not support buy now option."
            );
        
        public static readonly Error ItemAlreadyInAuction = 
            Error.Conflict("Auction.ItemAlreadyInAuction", 
                "This item is already in an active auction and cannot be used for another auction.");

        public static readonly Error ItemAlreadyHasAuction =
            Error.Conflict("Auction.ItemAlreadyHasAuction",
                "This item already has an auction in a commercial state and cannot be used to create another auction.");

        public static readonly Error PaymentDefaultedRequiresRelist =
            Error.Conflict("Auction.PaymentDefaultedRequiresRelist",
                "This item has a payment-defaulted auction. Use relist instead of creating a new auction.");

        public static readonly Error ItemRequiresMedia =
            Error.Conflict("Auction.ItemRequiresMedia",
                "Item must have at least one image before creating an auction.");
        
        public static readonly Error OnlyOwnerOfItem = 
            Error.Forbidden("Auction.OnlyOwnerOfItem", 
                "Only the owner of the item can create an auction for it.");
        
        public static readonly Error OnlyOwnerCanCancel =
            Error.Forbidden("Auction.OnlyOwnerCanCancel",
                "Only the auction owner can cancel.");

        public static Error CancelBlockedActiveBids(AuctionId id, int activeBidCount) =>
            Error.Conflict("Auction.CancelBlocked.ActiveBids",
                $"Cannot cancel auction '{id}' because it has {activeBidCount} active bid(s). " +
                "Use the admin emergency-terminate path for live auctions with bidders.");

public static readonly Error NoRunnerUp =
            Error.NotFound("Auction.NoRunnerUp",
                "No eligible runner-up bidder found.");

        public static readonly Error AlreadyResolved =
            Error.Conflict("Auction.AlreadyResolved",
                "Auction has already been resolved.");

        public static readonly Error TimingRequired =
            Error.Validation("Timing", "Auction.TimingRequired",
                "Auction timing (start/end time) must be set before scheduling.");

        public static readonly Error TimingAlreadySet =
            Error.Conflict("Auction.TimingAlreadySet",
                "Auction timing has already been set.");

        public static readonly Error CannotEdit =
            Error.Conflict("Auction.CannotEdit",
                "Auction cannot be edited in its current state.");

        public static readonly Error InvalidAuctionType =
            Error.Validation("AuctionType", "Auction.InvalidAuctionType",
                "Auction type is not supported.");

        public static readonly Error CannotSubmit =
            Error.Conflict("Auction.CannotSubmit",
                "Auction cannot be submitted in its current state. It must be in draft.");

        public static readonly Error CannotSetTiming =
            Error.Conflict("Auction.CannotSetTiming",
                "Auction timing can only be set when the auction is approved.");

        public static Error MaxRejectionsReached(int max) =>
            Error.Conflict("Auction.MaxRejectionsReached",
                $"This auction has been rejected {max} times and cannot be resubmitted.");

        public static readonly Error QualificationWindowRequired =
            Error.Validation("Qualification", "Auction.QualificationWindowRequired",
                "This auction requires a qualification window.");

        public static readonly Error PaymentDefaultRequiresWinner =
            Error.Conflict("Auction.PaymentDefaultRequiresWinner",
                "Auction payment default can only be applied when there is an existing winner.");

        public static readonly Error WinnerOfferAlreadyActive =
            Error.Conflict("Auction.WinnerOfferAlreadyActive",
                "An active runner-up offer already exists for this auction.");

        public static readonly Error InvalidWinnerOfferState =
            Error.Conflict("Auction.InvalidWinnerOfferState",
                "Runner-up offer is not in a state that allows this action.");

        public static readonly Error WinnerOfferExpired =
            Error.Conflict("Auction.WinnerOfferExpired",
                "Runner-up offer has already expired.");

        public static readonly Error NoMoreRunnerUps =
            Error.NotFound("Auction.NoMoreRunnerUps",
                "No additional eligible runner-up bidder is available.");

        public static readonly Error EmergencyBlockedByShipment =
            Error.Conflict("Auction.EmergencyBlockedByShipment",
                "Auction emergency cannot auto-terminate after shipment pickup. Use dispute flow instead.");

        public static readonly Error AlreadyRelisted =
            Error.Conflict("Auction.AlreadyRelisted",
                "This auction has already been relisted from the current payment-defaulted state.");

        public static readonly Error BuyNowReservationActive =
            Error.Conflict("Auction.BuyNowReservationActive",
                "This auction is temporarily reserved for a buy-now checkout.");

        public static readonly Error BuyNowUnavailableForScheduledAuction =
            Error.Conflict("Auction.BuyNowUnavailableForScheduledAuction",
                "Buy Now is not available for this auction.");

        public static readonly Error BuyNowQualificationClosed =
            Error.Conflict("Auction.BuyNowQualificationClosed",
                "The qualification (deposit) period for this auction has ended. Buy Now is unavailable until the auction starts.");

        public static readonly Error BuyNowQualificationNotOpenYet =
            Error.Conflict("Auction.BuyNowQualificationNotOpenYet",
                "The qualification (deposit) period for this auction has not started yet.");
    }

    public static class Bid
    {
        public static Error TooLow(Money bidAmount, Money minimumRequired) => 
            Error.Validation(
                "Amount",
                "Bid.TooLow", 
                $"Bid amount {bidAmount} is below minimum required {minimumRequired}.");

        public static readonly Error DepositRequired = 
            Error.Forbidden("Bid.DepositRequired", 
                "A deposit is required to participate in this auction.");

        public static Error NotFound(BidId id) => 
            Error.NotFound("Bid.NotFound", $"Bid with Id '{id}' was not found.");

        public static readonly Error BidAlreadyOutbid = 
            Error.Conflict("Bid.Outbid", "This bid has already been outbid.");

        public static readonly Error LiveBiddingUnavailableForSealedAuction =
            Error.Conflict("Bid.SealedAuctionOnly",
                "This auction only accepts sealed bids.");
    }

    public static class AutoBid
    {
        public static Error NotFound(AutoBidId id) => 
            Error.NotFound("AutoBid.NotFound", $"Auto bid with Id '{id}' was not found.");
        
        public static Error NotFoundForBidder(UserId bidderId) => 
            Error.NotFound("AutoBid.NotFound", $"No auto-bid found for bidder '{bidderId}'.");

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
        
        public static Error NewMaxLessThanCurrent(decimal currentAmount) =>
            Error.Conflict("AutoBid.NewMaxLessThanCurrent", 
                $"New maximum amount cannot be less than current amount of {currentAmount}.");
        public static Error InvalidInput(string details) => 
            Error.Validation("Input", "AutoBid.InvalidInput", 
                $"Invalid auto bid configuration: {details}");

        public static readonly Error AlreadyExists = 
            Error.Conflict("AutoBid.AlreadyExists", 
                "An auto bid already exists for this bidder on this auction.");

        public static readonly Error InvalidMaxAmount = 
            Error.Validation("MaxAmount", "AutoBid.InvalidMaxAmount", 
                "Maximum amount must be greater than zero.");
        
        public static Error MaxAmountMustGreaterAuctionCurrenPrice(Money maxAmount, Money auctionCurrentPrice) => 
            Error.Validation("MaxAmount", "AutoBid.InvalidMaxAmount", 
                $"Max amount ({maxAmount}) must be greater than current price ({auctionCurrentPrice}).");

        public static readonly Error InvalidCurrentAmount = 
            Error.Validation("CurrentAmount", "AutoBid.InvalidCurrentAmount", 
                "Current amount must not exceed maximum amount.");
        
        public static readonly Error CannotBid = 
            Error.Conflict("AutoBid.CannotBid", 
                "This auto bid cannot place a bid at this time, likely due to insufficient max amount or being disabled.");
    }

    public static class Deposit
    {
        public static Error InvalidInput(string details) => 
            Error.Validation("Input", "Deposit.InvalidInput", $"Invalid Deposit input: {details}");

        public static Error NotFound(AuctionDepositId id) => 
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

    public static class BuyNowReservation
    {
        public static Error NotFound(AuctionBuyNowReservationId id) =>
            Error.NotFound("AuctionBuyNowReservation.NotFound",
                $"Buy-now reservation '{id}' was not found.");

        public static Error InvalidInput(string details) =>
            Error.Validation("BuyNowReservation", "AuctionBuyNowReservation.InvalidInput", details);

        public static readonly Error InvalidFundingSplit =
            Error.Validation("BuyNowReservation", "AuctionBuyNowReservation.InvalidFundingSplit",
                "Deposit-applied amount and gateway amount must match the buy-now price.");

        public static readonly Error InvalidExpiration =
            Error.Validation("ExpiresAt", "AuctionBuyNowReservation.InvalidExpiration",
                "Buy-now reservation expiration must be in the future.");

        public static Error InvalidState(string currentState, string attemptedAction) =>
            Error.Conflict(
                "AuctionBuyNowReservation.InvalidState",
                $"Cannot perform '{attemptedAction}' when reservation status is '{currentState}'.");

        public static readonly Error Expired =
            Error.Conflict("AuctionBuyNowReservation.Expired",
                "Buy-now reservation has already expired.");
    }

    public static class Participant
    {
        public static readonly Error AlreadyJoined =
            Error.Conflict("Participant.AlreadyJoined",
                "User is already participating in this auction.");

        public static readonly Error NotFound =
            Error.NotFound("Participant.NotFound",
                "Auction participant was not found.");

        public static readonly Error JoinWindowClosed =
            Error.Conflict("Participant.JoinWindowClosed",
                "The qualification window is closed for this auction.");
        
        public static readonly Error JoinWindowNotOpenYet =
            Error.Conflict("Participant.JoinWindowNotOpenYet",
                "The qualification window is not open for this auction yet.");

        public static readonly Error NotQualified =
            Error.Forbidden("Participant.NotQualified",
                "Bidder is not qualified to participate in this auction.");
    }

    public static class SealedBid
    {
        public static readonly Error OnlySupportedForSealedAuction =
            Error.Conflict("SealedBid.UnsupportedAuctionType",
                "Sealed bids are only supported for sealed-bid auctions.");

        public static readonly Error AlreadySubmitted =
            Error.Conflict("SealedBid.AlreadySubmitted",
                "Bidder has already submitted a sealed bid for this auction.");

        public static readonly Error NotFound =
            Error.NotFound("SealedBid.NotFound",
                "Sealed bid was not found.");

        public static readonly Error RevealNotAllowed =
            Error.Conflict("SealedBid.RevealNotAllowed",
                "Sealed bid cannot be revealed before the auction has ended.");
    }

    public static class Emergency
    {
        public static readonly Error NotFound =
            Error.NotFound("AuctionEmergency.NotFound",
                "Auction emergency record was not found.");

        public static readonly Error AlreadyTriggered =
            Error.Conflict("AuctionEmergency.AlreadyTriggered",
                "Auction emergency is already active.");
    }

    public static class Watcher
    {
        public static Error NotFound(AuctionId auctionId, UserId userId) => 
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
        public static Error NotFound(ItemId id) => 
            Error.NotFound("Item.NotFound", $"Item with Id '{id}' was not found.");
        
        public static Error MediaNotFound(ItemMediaId id) => Error.NotFound(
            "Item.MediaNotFound", 
            $"Item media with Id '{id}' was not found.");
        public static Error QuestionNotFound(ItemQuestionId questionId) => 
            Error.NotFound(
                "Item.Question.NotFound", 
                $"Question with Id '{questionId}' was not found for this item.");
        public static Error AskOwnItem(ItemId itemId) => 
            Error.Forbidden(
                "Item.AskOwnItem", 
                $"Sellers cannot ask questions on their own item '{itemId}'.");
        
        public static readonly Error QuestionAnswered = 
            Error.Conflict(
                "Item.Question.Answered", 
                "Question has already been answered.");

        public static readonly Error MaxImagesReached = 
            Error.Validation("Images", "Item.MaxImagesReached", 
                "Maximum of 10 images reached for this item.");

        public static readonly Error InvalidCondition = 
            Error.Validation("Condition", "Item.InvalidCondition", 
                "Item condition must be one of the allowed values.");
        
        public static Error InvalidState(string currentState, string attemptedAction) => 
            Error.Conflict(
                "Item.InvalidState", 
                $"Cannot perform '{attemptedAction}' when item status is '{currentState}'.");

        public static Error CannotActivate(string reason) => 
            Error.Conflict(
                "Item.CannotActivate", 
                "Item cannot be activated: " + reason);

        public static Error CannotAuction(ItemId itemId, string currentStatus) => 
            Error.Conflict(
                "Item.NotAvailable", 
                $"Item '{itemId}' is not available for auction. Current status: '{currentStatus}'.");
        
        public static Error NotOwnedByUser(ItemId itemId, UserId userId) => 
            Error.Forbidden(
                "Item.NotOwnedByUser", 
                $"User '{userId}' does not own item '{itemId}'.");
        
        public static Error QuestionLimitReached(ItemId itemId, int maxQuestion) => 
            Error.Conflict(
                "Item.QuestionLimitReached", 
                $"Item '{itemId}' has reached the maximum number of questions ({maxQuestion}).");
    }
    
    public static class Category
    {
        public static Error NotFound(CategoryId id) => 
            Error.NotFound("Category.NotFound", $"Category with Id '{id}' was not found.");
        
        public static Error NotFoundWithSlug(string slug) => 
            Error.NotFound("Category.NotFound", $"Category with slug '{slug}' was not found.");
        
        public static Error SlugAlreadyExists(string slug) => 
            Error.Conflict("Category.SlugAlreadyExists", 
                $"A category with slug '{slug}' already exists.");
    }

}

using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class AuctionStatus : EnumValueObject<AuctionStatus>
{
    public static readonly AuctionStatus Draft = new("draft");
    public static readonly AuctionStatus Pending = new("pending");
    public static readonly AuctionStatus Approved = new("approved");
    public static readonly AuctionStatus Scheduled = new("scheduled");
    public static readonly AuctionStatus Active = new("active");
    public static readonly AuctionStatus Ended = new("ended");
    public static readonly AuctionStatus Sold = new("sold");
    public static readonly AuctionStatus Completed = new("completed");
    public static readonly AuctionStatus PaymentDefaulted = new("payment_defaulted");
    public static readonly AuctionStatus Cancelled = new("cancelled");
    public static readonly AuctionStatus Failed = new("failed");
    public static readonly AuctionStatus Terminated = new("terminated");

    public AuctionStatus(string id) : base(id) { }

    public bool AcceptsBids => this == Active;

    /// <summary>
    /// True when the auction has reached a successfully-closed outcome —
    /// either the winner was just resolved (<see cref="Sold"/>) or delivery is confirmed
    /// and the auction is terminal (<see cref="Completed"/>).
    /// Use this at query/read call-sites that want "the auction concluded with a winner",
    /// regardless of whether post-winner delivery has finished.
    /// </summary>
    public bool IsSuccessfullyClosed => this == Sold || this == Completed;

    /// <summary>
    /// True only while the winner is resolved but post-winner work (payment, delivery)
    /// is still in-flight (i.e., <see cref="Sold"/>). Call-sites that must short-circuit
    /// on the transient winner-resolved state (payment URLs, VnPay callbacks, bid guards)
    /// should use this instead of <see cref="IsSuccessfullyClosed"/>.
    /// </summary>
    public bool IsPostWinnerTransient => this == Sold;

    public bool CanTransitionTo(AuctionStatus target) =>
        (Id, target.Id) switch
        {
            ("draft", "pending") => true,
            ("draft", "cancelled") => true,
            ("pending", "approved") => true,
            ("pending", "cancelled") => true,
            ("approved", "scheduled") => true,
            ("approved", "cancelled") => true,
            ("scheduled", "active") => true,
            ("scheduled", "cancelled") => true,
            // Bug #10 fix: removed `scheduled → sold` — no production caller exercises it,
            // and allowing it would let an auction skip the entire Active/Ended bidding cycle.
            ("scheduled", "terminated") => true,
            ("active", "ended") => true,
            ("active", "cancelled") => true,
            ("active", "terminated") => true,
            ("active", "sold") => true,
            ("ended", "sold") => true,
            ("ended", "failed") => true,
            ("sold", "payment_defaulted") => true,
            ("payment_defaulted", "sold") => true,
            ("payment_defaulted", "scheduled") => true,
            ("sold", "completed") => true,
            ("sold", "terminated") => true,
            ("completed", "terminated") => true,
            ("payment_defaulted", "terminated") => true,
            ("payment_defaulted", "failed") => true,
            ("ended", "terminated") => true,
            _ => false
        };
}

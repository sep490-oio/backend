using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class User
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = null!;

    public string? NormalizedUserName { get; set; }

    public string Email { get; set; } = null!;

    public string? NormalizedEmail { get; set; }

    public bool EmailConfirmed { get; set; }

    public DateTime? EmailConfirmedAt { get; set; }

    public string? PasswordHash { get; set; }

    public string? PhoneNumber { get; set; }

    public string? PhoneNumberCountryCode { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public DateTime? PhoneNumberConfirmedAt { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public string TwoFactorProvider { get; set; } = null!;

    public string Status { get; set; } = null!;

    public bool LockoutEnabled { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public short AccessFailedCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int Version { get; set; }

    public virtual ICollection<AuctionAutoBid> AuctionAutoBids { get; set; } = new List<AuctionAutoBid>();

    public virtual ICollection<AuctionDeposit> AuctionDeposits { get; set; } = new List<AuctionDeposit>();

    public virtual ICollection<AuctionWatcher> AuctionWatchers { get; set; } = new List<AuctionWatcher>();

    public virtual ICollection<Auction> Auctions { get; set; } = new List<Auction>();

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual ICollection<BuyerReview> BuyerReviewBuyers { get; set; } = new List<BuyerReview>();

    public virtual ICollection<BuyerReview> BuyerReviewReviewers { get; set; } = new List<BuyerReview>();

    public virtual ICollection<Dispute> DisputeAssignedToNavigations { get; set; } = new List<Dispute>();

    public virtual ICollection<Dispute> DisputeComplainants { get; set; } = new List<Dispute>();

    public virtual ICollection<Dispute> DisputeEscalatedToNavigations { get; set; } = new List<Dispute>();

    public virtual ICollection<DisputeEvidence> DisputeEvidences { get; set; } = new List<DisputeEvidence>();

    public virtual ICollection<DisputeMessage> DisputeMessages { get; set; } = new List<DisputeMessage>();

    public virtual ICollection<DisputeRefund> DisputeRefunds { get; set; } = new List<DisputeRefund>();

    public virtual ICollection<Dispute> DisputeRespondents { get; set; } = new List<Dispute>();

    public virtual ICollection<DisputeStatusHistory> DisputeStatusHistories { get; set; } = new List<DisputeStatusHistory>();

    public virtual ICollection<Invoice> InvoiceBuyers { get; set; } = new List<Invoice>();

    public virtual ICollection<Invoice> InvoiceSellers { get; set; } = new List<Invoice>();

    public virtual ICollection<ItemQuestion> ItemQuestions { get; set; } = new List<ItemQuestion>();

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    public virtual ICollection<NotificationDelivery> NotificationDeliveries { get; set; } = new List<NotificationDelivery>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Order> OrderBuyers { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderSellers { get; set; } = new List<Order>();

    public virtual ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();

    public virtual ICollection<ReviewReport> ReviewReportReporters { get; set; } = new List<ReviewReport>();

    public virtual ICollection<ReviewReport> ReviewReportReviewedByNavigations { get; set; } = new List<ReviewReport>();

    public virtual ICollection<ReviewVote> ReviewVotes { get; set; } = new List<ReviewVote>();

    public virtual ICollection<SellerKycHistory> SellerKycHistories { get; set; } = new List<SellerKycHistory>();

    public virtual ICollection<SellerKyc> SellerKycs { get; set; } = new List<SellerKyc>();

    public virtual SellerProfile? SellerProfile { get; set; }

    public virtual SellerRatingSummary? SellerRatingSummary { get; set; }

    public virtual ICollection<SellerReview> SellerReviewReviewers { get; set; } = new List<SellerReview>();

    public virtual ICollection<SellerReview> SellerReviewSellers { get; set; } = new List<SellerReview>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual UserAddress? UserAddress { get; set; }

    public virtual ICollection<UserLoginHistory> UserLoginHistories { get; set; } = new List<UserLoginHistory>();

    public virtual UserNotificationPreference? UserNotificationPreference { get; set; }

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

    public virtual UserProfile? UserProfile { get; set; }

    public virtual ICollection<UserRefreshTokenFamily> UserRefreshTokenFamilies { get; set; } = new List<UserRefreshTokenFamily>();

    public virtual ICollection<UserRefreshToken> UserRefreshTokens { get; set; } = new List<UserRefreshToken>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual Wallet? Wallet { get; set; }

    public virtual ICollection<WithdrawalRequest> WithdrawalRequestProcessedByNavigations { get; set; } = new List<WithdrawalRequest>();

    public virtual ICollection<WithdrawalRequest> WithdrawalRequestUsers { get; set; } = new List<WithdrawalRequest>();
}

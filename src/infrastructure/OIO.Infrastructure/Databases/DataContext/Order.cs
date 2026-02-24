using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Order
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = null!;

    public Guid AuctionId { get; set; }

    public Guid BuyerId { get; set; }

    public Guid SellerId { get; set; }

    public Guid? ShippingAddressId { get; set; }

    public string? ShippingRecipientName { get; set; }

    public string? ShippingPhone { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public string? ShippingWard { get; set; }

    public string? ShippingDistrict { get; set; }

    public string? ShippingCity { get; set; }

    public Guid? BillingAddressId { get; set; }

    public decimal ItemPrice { get; set; }

    public decimal? ShippingFee { get; set; }

    public decimal? PlatformFee { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Currency { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public int Version { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Auction Auction { get; set; } = null!;

    public virtual UserAddress? BillingAddress { get; set; }

    public virtual User Buyer { get; set; } = null!;

    public virtual ICollection<BuyerReview> BuyerReviews { get; set; } = new List<BuyerReview>();

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual ICollection<Escrow> Escrows { get; set; } = new List<Escrow>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual User Seller { get; set; } = null!;

    public virtual ICollection<SellerReview> SellerReviews { get; set; } = new List<SellerReview>();

    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    public virtual UserAddress? ShippingAddressNavigation { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

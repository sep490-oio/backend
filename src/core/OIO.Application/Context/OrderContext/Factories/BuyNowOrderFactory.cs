using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Factories;

/// <summary>
/// Builds a pending_payment Order for a buy-now reservation. Shared by the
/// order-first BuyNowCommand path and the legacy VnPay-callback path. Uses a
/// placeholder shipping snapshot when the buyer has no address yet — the
/// buyer can finish the address from the order detail page via
/// Order.UpdateShipping while the order is still PendingPayment / Paid.
/// </summary>
public static class BuyNowOrderFactory
{
    public static Result<Order, Error> Create(
        Auction auction,
        User buyer,
        AuctionBuyNowReservation reservation,
        DateTime nowUtc,
        string? notes = null)
    {
        var shippingAddress = buyer.Addresses.FirstOrDefault(x => x.IsDefault)
                              ?? buyer.Addresses.FirstOrDefault();

        ShippingSnapshot shipping;
        if (shippingAddress is not null)
        {
            var structured = ShippingSnapshot.CreateStructured(
                recipientName: shippingAddress.Recipient.RecipientName,
                phone: shippingAddress.Recipient.Phone.Value,
                street: shippingAddress.Address.Street,
                ward: shippingAddress.Address.Ward,
                district: shippingAddress.Address.District,
                city: shippingAddress.Address.City,
                postalCode: shippingAddress.Address.PostalCode);

            shipping = structured.IsSuccess
                ? structured.Value
                : ShippingSnapshot.Create(
                    recipientName: shippingAddress.Recipient.RecipientName,
                    phone: shippingAddress.Recipient.Phone.Value,
                    address: shippingAddress.Address.Street,
                    ward: shippingAddress.Address.Ward,
                    district: shippingAddress.Address.District,
                    city: shippingAddress.Address.City);
        }
        else
        {
            shipping = ShippingSnapshot.Create(
                recipientName: ResolveUserDisplayName(buyer),
                phone: null,
                address: "Address pending update",
                ward: null,
                district: null,
                city: null);
        }

        var pricing = OrderPricing.Create(
            itemPrice: reservation.BuyNowPrice,
            shippingFee: 0m,
            platformFee: 0m,
            taxAmount: 0m,
            totalAmount: reservation.BuyNowPrice);

        return Order.Create(
            auctionId: auction.Id,
            buyerId: buyer.Id,
            sellerId: auction.Item.SellerId,
            shipping: shipping,
            shippingAddressId: shippingAddress?.Id,
            billingAddressId: shippingAddress?.Id,
            pricing: pricing,
            currency: reservation.BuyNowPrice.Currency.Id,
            // Order-first flow: payment must complete before the reservation
            // window closes, so the order's payment due deadline mirrors the
            // reservation expiry rather than nowUtc.
            paymentDueAt: reservation.ExpiresAt,
            nowUtc: nowUtc,
            isPlatformVerifiedItem: auction.Item.RequiresPlatformInspection,
            notes: notes);
    }

    public static string ResolveUserDisplayName(User user)
    {
        var displayName = user.Profile?.Name?.DisplayName?.Trim();
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;

        var fullName = user.Profile?.Name?.FullName?.Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return user.UserName.Value;
    }
}

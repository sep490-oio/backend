using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.OrderContext.Enums;

public sealed class SellerDirectShipmentEvidenceKind : EnumValueObject<SellerDirectShipmentEvidenceKind>
{
    public static readonly SellerDirectShipmentEvidenceKind SellerPackagePhoto = new("seller_package_photo");
    public static readonly SellerDirectShipmentEvidenceKind SellerHandoverProof = new("seller_handover_proof");
    public static readonly SellerDirectShipmentEvidenceKind BuyerDeliveryPhoto = new("buyer_delivery_photo");

    private SellerDirectShipmentEvidenceKind(string id) : base(id) { }
}

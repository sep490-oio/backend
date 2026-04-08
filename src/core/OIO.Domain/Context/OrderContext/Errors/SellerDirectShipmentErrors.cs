using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.OrderContext.Errors;

public static class SellerDirectShipmentErrors
{
    public static class SellerDirectShipment
    {
        public static Error NotFound(SellerDirectShipmentId id)
            => Error.NotFound("SellerDirectShipment.NotFound", $"Seller direct shipment {id} not found.");

        public static Error InvalidTransition(string currentStatus, string action)
            => Error.Validation(string.Empty, "SellerDirectShipment.InvalidTransition",
                $"Cannot {action} because shipment is in {currentStatus} state.");

        public static Error CarrierInfoRequired
            => Error.Validation(string.Empty, "SellerDirectShipment.CarrierInfoRequired",
                "Carrier information must be set before marking shipment as picked up.");

        public static Error CarrierInfoLocked
            => Error.Conflict("SellerDirectShipment.CarrierInfoLocked",
                "Carrier information is locked once the shipment has been picked up.");

        public static Error DispatchDetailsIncomplete
            => Error.Validation(string.Empty, "SellerDirectShipment.DispatchDetailsIncomplete",
                "Dispatch details are incomplete: carrier info and at least one package photo are required before marking picked up.");
    }
}

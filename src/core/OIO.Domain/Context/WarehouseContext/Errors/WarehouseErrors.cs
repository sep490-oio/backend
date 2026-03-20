using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.WarehouseContext.Errors;

public static class WarehouseErrors
{
    public static class InboundShipment
    {
        public static Error NotFound(string id) => Error.NotFound(
            code: "InboundShipment.NotFound",
            description: $"Inbound shipment '{id}' was not found.");
 
        public static readonly Error AlreadyBooked = Error.Conflict(
            code: "InboundShipment.AlreadyBooked",
            description: "This inbound shipment has already been booked with a carrier.");
 
        public static readonly Error AlreadyArrived = Error.Conflict(
            code: "InboundShipment.AlreadyArrived",
            description: "This inbound shipment has already arrived at the warehouse.");
 
        public static readonly Error CannotInspect = Error.Conflict(
            code: "InboundShipment.CannotInspect",
            description: "Shipment must be in 'arrived' status before it can be inspected.");
 
        public static readonly Error CannotComplete = Error.Conflict(
            code: "InboundShipment.CannotComplete",
            description: "Shipment must be in 'inspected' status before it can be completed.");
 
        public static readonly Error CannotCancel = Error.Conflict(
            code: "InboundShipment.CannotCancel",
            description: "Shipment cannot be cancelled in its current status.");
 
        public static readonly Error NotExternalCarrier = Error.Conflict(
            code: "InboundShipment.NotExternalCarrier",
            description: "Only external carrier shipments can have their status manually advanced.");
 
        public static readonly Error NotPlatformManaged = Error.Conflict(
            code: "InboundShipment.NotPlatformManaged",
            description: "This operation is only valid for platform-managed shipments.");
 
        public static readonly Error StatusUnchanged = Error.Conflict(
            code: "InboundShipment.StatusUnchanged",
            description: "The shipment is already in the requested status.");
 
        public static readonly Error InvalidTransition = Error.Conflict(
            code: "InboundShipment.InvalidTransition",
            description: "The requested status transition is not allowed.");
 
        public static readonly Error ExternalCarrierNameRequired = 
        Error.Validation("Inbound", "InboundShipment.ExternalCarrierNameRequired", 
            "ExternalCarrierName is required for external carrier shipments.");
 
        public static readonly Error ExternalTrackingAlreadySet = Error.Conflict(
            code: "InboundShipment.ExternalTrackingAlreadySet",
            description: "A tracking number has already been set for this shipment.");
 
        public static readonly Error TrackingNotAllowedForExternal = Error.Conflict(
            code: "InboundShipment.TrackingNotAllowedForExternal",
            description: "Carrier webhook tracking is not applicable to external carrier shipments.");

        public static readonly Error SenderAddressMissingAndNoDefault = Error.Validation(
            "SenderAddress",
            "InboundShipment.SenderAddressMissingAndNoDefault",
            "Sender address information is missing, and the user has no default address configured.");
    }

    public static class WarehouseItem
    {
        public static Error NotFound(string id) => Error.NotFound(
            code: "WarehouseItem.NotFound",
            description: $"Warehouse item '{id}' was not found.");

        public static readonly Error NotReceived = Error.Conflict(
            code: "WarehouseItem.NotReceived",
            description: "Item must be received before this operation is allowed.");

        public static readonly Error AlreadyStored = Error.Conflict(
            code: "WarehouseItem.AlreadyStored",
            description: "This item is already stored in a location.");

        public static readonly Error AlreadyInspected = Error.Conflict(
            code: "WarehouseItem.AlreadyInspected",
            description: "This item has already been inspected.");

        public static readonly Error NotInspected = Error.Conflict(
            code: "WarehouseItem.NotInspected",
            description: "Item must be inspected before it can be stored.");

        public static readonly Error NotAvailable = Error.Conflict(
            code: "WarehouseItem.NotAvailable",
            description: "Item is not available for dispatch — it may already be reserved or dispatched.");

        public static readonly Error LocationOccupied = Error.Conflict(
            code: "WarehouseItem.LocationOccupied",
            description: "The specified storage location is already occupied.");
    }

    public static class OutboundShipment
    {
        public static Error NotFound(string id) => Error.NotFound(
            code: "OutboundShipment.NotFound",
            description: $"Outbound shipment '{id}' was not found.");

        public static readonly Error AlreadyBooked = Error.Conflict(
            code: "OutboundShipment.AlreadyBooked",
            description: "This outbound shipment has already been booked with a carrier.");

        public static readonly Error CannotCancel = Error.Conflict(
            code: "OutboundShipment.CannotCancel",
            description: "Shipment cannot be cancelled once it has been picked up.");

        public static readonly Error CannotMarkDelivered = Error.Conflict(
            code: "OutboundShipment.CannotMarkDelivered",
            description: "Shipment must be in transit before it can be marked as delivered.");

        public static readonly Error NotSellerSelfShip = Error.Conflict(
            code: "OutboundShipment.NotSellerSelfShip",
            description: "This operation is only valid for seller self-ship shipments.");

        public static readonly Error AlreadyShipped = Error.Conflict(
            code: "OutboundShipment.AlreadyShipped",
            description: "A tracking number has already been set for this shipment.");

        public static Error AlreadyExists(string orderId) => Error.Conflict(
            code: "OutboundShipment.AlreadyExists",
            description: $"An outbound shipment already exists for order '{orderId}'.");
    }

    public static class ShippingProvider
    {
        public static Error NotFound(string providerCode) => Error.NotFound(
            code: "ShippingProvider.NotFound",
            description: $"No active shipping provider found for code '{providerCode}'.");

        public static readonly Error NoDefaultProvider = Error.NotFound(
            code: "ShippingProvider.NoDefault",
            description: "No default shipping provider is configured.");

        public static readonly Error ProviderInactive = Error.Conflict(
            code: "ShippingProvider.Inactive",
            description: "The specified shipping provider is currently inactive.");
    }

    public static class StorageLocation
    {
        public static Error NotFound(string id) => Error.NotFound(
            code: "StorageLocation.NotFound",
            description: $"Storage location '{id}' was not found.");

        public static readonly Error Occupied = Error.Conflict(
            code: "StorageLocation.Occupied",
            description: "This storage location is already occupied.");
    }
}
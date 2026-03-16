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
    }

    public static class WarehouseItem
    {
        public static Error NotFound(string id) => Error.NotFound(
            code: "WarehouseItem.NotFound",
            description: $"Warehouse item '{id}' was not found.");

        public static readonly Error AlreadyStored = Error.Conflict(
            code: "WarehouseItem.AlreadyStored",
            description: "This item is already stored in a location.");
        public static readonly Error AlreadyInspected = Error.Conflict(
            code: "WarehouseItem.AlreadyInspected",
            description: "This item is already AlreadyInspected.");
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

    public static class Inspection
    {
        public static Error NotFound(string id) => Error.NotFound(
            code: "WarehouseInspection.NotFound",
            description: $"Warehouse inspection '{id}' was not found.");

        public static readonly Error AlreadyExists = Error.Conflict(
            code: "WarehouseInspection.AlreadyExists",
            description: "An inspection record already exists for this inbound shipment.");

        public static readonly Error EvidenceRequired = Error.Validation(
            "InspectionMediaUploadIds",
            "WarehouseInspection.EvidenceRequired",
            "At least one inspection image is required.");

        public static readonly Error CannotReview = Error.Conflict(
            code: "WarehouseInspection.CannotReview",
            description: "This inspection is not ready for review.");

        public static readonly Error AlreadyReviewed = Error.Conflict(
            code: "WarehouseInspection.AlreadyReviewed",
            description: "This inspection has already been reviewed.");

        public static readonly Error ConditionConfirmationNotRequired = Error.Conflict(
            code: "WarehouseInspection.ConditionConfirmationNotRequired",
            description: "This inspection does not require seller condition confirmation.");

        public static readonly Error UnsupportedApprovalCondition = Error.Validation(
            "ConditionId",
            "WarehouseInspection.UnsupportedApprovalCondition",
            "This inspected condition cannot be approved for listing confirmation.");
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

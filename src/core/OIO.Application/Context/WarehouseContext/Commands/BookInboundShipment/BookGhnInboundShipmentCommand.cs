using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.BookInboundShipment;

public sealed record GhnMetadata(int Id, string Code);

/// <summary>
/// Specialized command for GHN inbound booking that includes specific DistrictID and WardCode metadata.
/// </summary>
public sealed record BookGhnInboundShipmentCommand(
    List<BookInboundShipmentItem> Items,
    int     WeightGrams,
    decimal InsuranceValue,
    GhnMetadata? SenderMetadata = null,
    string? SenderName                   = null,
    string? SenderPhone                  = null,
    string? SenderAddress                = null,
    string? SenderWard                   = null,
    string? SenderDistrict               = null,
    string? SenderProvince               = null,
    int?    LengthCm                       = null,
    int?    WidthCm                        = null,
    int?    HeightCm                       = null,
    string? SenderCarrierAddressDataJson   = null,
    string? Notes                          = null,
    string? GhnHandlingNote                = null
) : ICommand<List<InboundShipmentDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        var check = BookGhnInboundShipmentCommand.Check()
            .WithOwnerName("BookGhnInboundShipment")
            .Field(Items, nameof(Items)).NotNull()
            .Field(Items?.Count ?? 0, nameof(Items)).GreaterThan(0)
            .Field(WeightGrams).GreaterThan(0)
            .Field(InsuranceValue).NonNegative();

        if (SenderMetadata != null)
        {
            check.Field(SenderMetadata.Id, "SenderMetadata.Id").GreaterThan(0);
            check.Field(SenderMetadata.Code, "SenderMetadata.Code").NotNullOrWhiteSpace();
        }

        return check;
    }
}

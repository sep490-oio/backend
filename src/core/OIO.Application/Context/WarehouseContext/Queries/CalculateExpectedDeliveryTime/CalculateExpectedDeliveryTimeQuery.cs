using CSharpFunctionalExtensions;
using MediatR;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.CalculateExpectedDeliveryTime;

public sealed record CalculateExpectedDeliveryTimeQuery : IRequest<Result<DateTime?, Error>>
{
    public required string ProviderCode { get; init; } // "ghn"
    
    public required string SenderDistrict { get; init; }
    public required string SenderProvince { get; init; }
    public string? SenderCarrierAddressDataJson { get; init; }

    public required string RecipientDistrict { get; init; }
    public required string RecipientProvince { get; init; }
    public string? RecipientCarrierAddressDataJson { get; init; }
}

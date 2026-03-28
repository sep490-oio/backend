using CSharpFunctionalExtensions;
using MediatR;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.CalculateShippingFee;

public sealed record CalculateShippingFeeQuery : IRequest<Result<decimal, Error>>
{
    public required string  ProviderCode { get; init; } // "ghn"
    public required int     WeightGrams  { get; init; }
    public decimal InsuranceValue { get; init; }
    public decimal CodAmount      { get; init; }

    // --- Recipient: warehouse flag, UserId, or explicit fields ---
    public bool     RecipientIsWarehouse { get; init; }
    public Guid?    RecipientUserId      { get; init; }
    public string?  RecipientDistrict    { get; init; }
    public string?  RecipientProvince    { get; init; }
    public string?  RecipientCarrierAddressDataJson { get; init; }

    public int? LengthCm { get; init; }
    public int? WidthCm  { get; init; }
    public int? HeightCm { get; init; }
}

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
    public required string  RecipientDistrict { get; init; }
    public required string  RecipientProvince { get; init; }
    public string? RecipientCarrierAddressDataJson { get; init; }
    public int? LengthCm { get; init; }
    public int? WidthCm  { get; init; }
    public int? HeightCm { get; init; }
}

using CSharpFunctionalExtensions;
using MediatR;
using OIO.Application.Context.AddressContext.DTOs;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AddressContext.Queries.GetDistricts;

public sealed record GetDistrictsQuery : IRequest<Result<List<DistrictDto>, Error>>
{
    public required int ProvinceId { get; init; }
}

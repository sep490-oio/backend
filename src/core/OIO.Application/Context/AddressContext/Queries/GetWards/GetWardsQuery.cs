using CSharpFunctionalExtensions;
using MediatR;
using OIO.Application.Context.AddressContext.DTOs;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AddressContext.Queries.GetWards;

public sealed record GetWardsQuery : IRequest<Result<List<WardDto>, Error>>
{
    public required int DistrictId { get; init; }
}

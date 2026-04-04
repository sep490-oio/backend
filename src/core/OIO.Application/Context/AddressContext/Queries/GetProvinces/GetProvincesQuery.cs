using CSharpFunctionalExtensions;
using MediatR;
using OIO.Application.Context.AddressContext.DTOs;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AddressContext.Queries.GetProvinces;

public sealed record GetProvincesQuery : IRequest<Result<List<ProvinceDto>, Error>>;

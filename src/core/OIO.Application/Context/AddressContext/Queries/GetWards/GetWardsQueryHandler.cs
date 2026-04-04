using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using OIO.Application.Context.AddressContext.DTOs;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AddressContext.Queries.GetWards;

internal sealed class GetWardsQueryHandler
    : IRequestHandler<GetWardsQuery, Result<List<WardDto>, Error>>
{
    private readonly IWebHostEnvironment _env;

    public GetWardsQueryHandler(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<Result<List<WardDto>, Error>> Handle(
        GetWardsQuery request, CancellationToken cancellationToken)
    {
        var wards = await AddressDataReader.ReadWardsAsync(
            _env, request.DistrictId, cancellationToken);
        return wards;
    }
}

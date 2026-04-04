using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using OIO.Application.Context.AddressContext.DTOs;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AddressContext.Queries.GetDistricts;

internal sealed class GetDistrictsQueryHandler
    : IRequestHandler<GetDistrictsQuery, Result<List<DistrictDto>, Error>>
{
    private readonly IWebHostEnvironment _env;

    public GetDistrictsQueryHandler(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<Result<List<DistrictDto>, Error>> Handle(
        GetDistrictsQuery request, CancellationToken cancellationToken)
    {
        var districts = await AddressDataReader.ReadDistrictsAsync(
            _env, request.ProvinceId, cancellationToken);
        return districts;
    }
}

using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using OIO.Application.Context.AddressContext.DTOs;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AddressContext.Queries.GetProvinces;

internal sealed class GetProvincesQueryHandler
    : IRequestHandler<GetProvincesQuery, Result<List<ProvinceDto>, Error>>
{
    private readonly IWebHostEnvironment _env;

    public GetProvincesQueryHandler(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<Result<List<ProvinceDto>, Error>> Handle(
        GetProvincesQuery request, CancellationToken cancellationToken)
    {
        var provinces = await AddressDataReader.ReadProvincesAsync(_env, cancellationToken);
        return provinces;
    }
}

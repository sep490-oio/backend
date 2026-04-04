using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AddressContext.DTOs;
using OIO.Application.Context.AddressContext.Queries.GetProvinces;

namespace OIO.Api.Endpoints.AddressContext;

public sealed class GetProvincesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Address.GetProvinces, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetProvincesQuery(), ct);
                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Address.GetProvinces)
            .WithTags(ApiEndpoint.Tags.Address)
            .Produces<List<ProvinceDto>>(StatusCodes.Status200OK);
    }
}

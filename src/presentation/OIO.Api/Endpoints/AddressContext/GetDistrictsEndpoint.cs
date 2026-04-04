using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AddressContext.DTOs;
using OIO.Application.Context.AddressContext.Queries.GetDistricts;

namespace OIO.Api.Endpoints.AddressContext;

public sealed class GetDistrictsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Address.GetDistricts, async (
                [Microsoft.AspNetCore.Mvc.FromQuery(Name = "province_id")] int provinceId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetDistrictsQuery { ProvinceId = provinceId }, ct);
                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Address.GetDistricts)
            .WithTags(ApiEndpoint.Tags.Address)
            .Produces<List<DistrictDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

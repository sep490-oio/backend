using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AddressContext.DTOs;
using OIO.Application.Context.AddressContext.Queries.GetWards;

namespace OIO.Api.Endpoints.AddressContext;

public sealed class GetWardsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Address.GetWards, async (
                [Microsoft.AspNetCore.Mvc.FromQuery(Name = "district_id")] int districtId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetWardsQuery { DistrictId = districtId }, ct);
                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Address.GetWards)
            .WithTags(ApiEndpoint.Tags.Address)
            .Produces<List<WardDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

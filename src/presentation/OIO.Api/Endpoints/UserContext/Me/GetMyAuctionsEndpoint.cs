using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.Filters;
using OIO.Application.Context.AuctionContext.Queries.GetMyAuctions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyAuctionsEndpoint : IEndpoint
{
    public sealed record Parameters : MyAuctionFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.MyAuctions, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMyAuctionsQuery(parameters);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetMyAuctions)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}
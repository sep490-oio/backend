using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetMyBids;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyBidsEndpoint : IEndpoint
{
    public sealed record Parameters : GetMyBidsFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.MyBids, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMyBidsQuery(parameters);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadBids)
            .WithName(ApiEndpoint.Names.Me.GetMyBids)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}
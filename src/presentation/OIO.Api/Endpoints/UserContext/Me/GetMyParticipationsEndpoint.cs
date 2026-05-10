using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetMyParticipations;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyParticipationsEndpoint : IEndpoint
{
    public sealed record Parameters : GetMyParticipationsFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.MyParticipations, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMyParticipationsQuery(parameters);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadBids)
            .WithName(ApiEndpoint.Names.Me.GetMyParticipations)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}

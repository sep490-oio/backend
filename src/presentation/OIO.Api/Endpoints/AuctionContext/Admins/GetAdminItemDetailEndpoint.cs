using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetItemById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class GetAdminItemDetailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetAdminItemDetail, async (
                Guid itemId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetItemByIdQuery(itemId);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadItems)
            .WithName(ApiEndpoint.Names.Admins.GetAdminItemDetail)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status200OK);
    }
}

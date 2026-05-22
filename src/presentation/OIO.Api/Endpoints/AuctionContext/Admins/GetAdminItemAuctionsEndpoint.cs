using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Queries.GetItemAuctions;
using OIO.Domain.AppDefinitions;
using CSharpFunctionalExtensions;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class GetAdminItemAuctionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetAdminItemAuctions, async (
                Guid itemId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetItemAuctionsQuery(itemId);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadItems)
            .WithName(ApiEndpoint.Names.Admins.GetAdminItemAuctions)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<List<AuctionListItemDto>>(StatusCodes.Status200OK);
    }
}

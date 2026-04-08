using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Queries.Admin.GetCompletedAuctions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.OrderContext.Admins;

public sealed class GetCompletedAuctionsEndpoint : IEndpoint
{
    public sealed record Parameters : GetCompletedAuctionsQueryFilter;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetCompletedAuctions, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetCompletedAuctionsQuery(parameters),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadItems)
            .WithName(ApiEndpoint.Names.Admins.GetCompletedAuctions)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<PagedList<AdminCompletedAuctionListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}

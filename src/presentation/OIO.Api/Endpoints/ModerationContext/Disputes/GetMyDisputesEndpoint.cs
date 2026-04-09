using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetMyDisputes;

namespace OIO.Api.Endpoints.ModerationContext.Disputes;

public sealed class GetMyDisputesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyDisputes, async (
                int? pageNumber,
                int? pageSize,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetMyDisputesQuery(
                        new PagedParameters
                        {
                            PageNumber = pageNumber,
                            PageSize = pageSize
                        }),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetMyDisputes)
            .WithTags(ApiEndpoint.Tags.Disputes)
            .Produces<PagedList<BuyerDisputeListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}

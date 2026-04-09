using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetMyDisputeById;

namespace OIO.Api.Endpoints.ModerationContext.Disputes;

public sealed class GetMyDisputeByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyDisputeById, async (
                Guid disputeId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetMyDisputeByIdQuery(disputeId), ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetMyDisputeById)
            .WithTags(ApiEndpoint.Tags.Disputes)
            .Produces<BuyerDisputeDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetDisputeThread;

namespace OIO.Api.Endpoints.ModerationContext.Disputes;

public sealed class GetDisputeThreadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Disputes.GetById, async (
                Guid disputeId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetDisputeThreadQuery(disputeId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Disputes.GetDisputeThread)
            .WithTags(ApiEndpoint.Tags.Disputes)
            .Produces<DisputeThreadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

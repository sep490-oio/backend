using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetDisputeMessages;

namespace OIO.Api.Endpoints.ModerationContext.Disputes;

public sealed class GetDisputeMessagesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Disputes.GetMessages, async (
                Guid disputeId,
                DateTime? beforeCreatedAt,
                Guid? beforeId,
                int? pageSize,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetDisputeMessagesQuery(disputeId, beforeCreatedAt, beforeId, pageSize),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Disputes.GetDisputeMessages)
            .WithTags(ApiEndpoint.Tags.Disputes)
            .Produces<DisputeMessagePageDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

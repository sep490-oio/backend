using MediatR;
using OIO.Api.Common;
using OIO.Api.Filters;
using OIO.Application.Context.ModerationContext.Commands.SendDisputeMessage;
using OIO.Application.Context.ModerationContext.DTOs;

namespace OIO.Api.Endpoints.ModerationContext.Disputes;

public sealed class SendDisputeMessageEndpoint : IEndpoint
{
    public sealed record Request(
        string? Message,
        IReadOnlyList<Guid>? MediaUploadIds = null,
        bool IsInternal = false);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Disputes.SendMessage, async (
                Guid disputeId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new SendDisputeMessageCommand(
                        disputeId,
                        request.Message,
                        request.MediaUploadIds,
                        request.IsInternal),
                    ct);
                return result.ToOkHttpResult();
            })
            .AddEndpointFilter(new IdempotencyFilter<DisputeMessageDto>(
                IdempotencyHttpPolicies.SendDisputeMessage()))
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Disputes.SendDisputeMessage)
            .WithTags(ApiEndpoint.Tags.Disputes)
            .Produces<DisputeMessageDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.TransitionDisputeStatus;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins.Disputes;

public sealed class TransitionDisputeStatusEndpoint : IEndpoint
{
    public sealed record Request([Required] string Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.TransitionDisputeStatus, async (
                Guid disputeId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new TransitionDisputeStatusCommand(disputeId, request.Status), ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.TransitionDisputeStatus)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

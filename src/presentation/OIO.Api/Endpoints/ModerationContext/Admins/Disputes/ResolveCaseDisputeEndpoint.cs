using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.ResolveCaseDispute;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins.Disputes;

public sealed class ResolveCaseDisputeEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Outcome,
        [Required] string Reason,
        ResolveCaseActionSetRequest? ActionSet = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.ResolveCaseDispute, async (
                Guid disputeId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ResolveCaseDisputeCommand(
                        disputeId,
                        request.Outcome,
                        request.Reason,
                        request.ActionSet),
                    ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.ResolveCaseDispute)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

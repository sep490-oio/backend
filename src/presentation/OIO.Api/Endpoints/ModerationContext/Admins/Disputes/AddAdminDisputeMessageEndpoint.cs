using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.AddAdminDisputeMessage;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins.Disputes;

public sealed class AddAdminDisputeMessageEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Content,
        string Visibility = "external");

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.AddAdminDisputeMessage, async (
                Guid disputeId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AddAdminDisputeMessageCommand(
                        disputeId,
                        request.Content,
                        request.Visibility),
                    ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.AddAdminDisputeMessage)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

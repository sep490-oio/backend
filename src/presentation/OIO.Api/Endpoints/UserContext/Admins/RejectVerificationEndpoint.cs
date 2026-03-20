using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.RejectVerification;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class RejectVerificationEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Reason,
        string? RejectionCode = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.RejectVerification, async (
                Guid verificationId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new RejectVerificationCommand(
                    verificationId, request.Reason, request.RejectionCode);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageVerifications)
            .WithName(ApiEndpoint.Names.Admins.RejectVerification)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}

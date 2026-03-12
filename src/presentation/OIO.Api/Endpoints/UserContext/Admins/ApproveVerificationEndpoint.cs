using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.ApproveVerification;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class ApproveVerificationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.ApproveVerification, async (
                Guid verificationId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ApproveVerificationCommand(verificationId);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageVerifications)
            .WithName(ApiEndpoint.Names.Admins.ApproveVerification)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}

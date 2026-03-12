using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.SubmitVerification;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class SubmitVerificationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.SubmitVerification, async (
                Guid verificationId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new SubmitVerificationCommand(verificationId);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageVerification)
            .WithName(ApiEndpoint.Names.Me.SubmitVerification)
            .WithTags(ApiEndpoint.Tags.Verifications);
    }
}

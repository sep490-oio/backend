using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.DeleteVerificationDocument;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class DeleteVerificationDocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoint.Url.Me.DeleteVerificationDocument, async (
                Guid verificationId,
                Guid docId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new DeleteVerificationDocumentCommand(verificationId, docId);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageVerification)
            .WithName(ApiEndpoint.Names.Me.DeleteVerificationDocument)
            .WithTags(ApiEndpoint.Tags.Verifications);
    }
}

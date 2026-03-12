using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.UploadVerificationDocument;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class UploadVerificationDocumentEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] Guid MediaUploadId,
        [Required] string DocumentType);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.UploadVerificationDocument, async (
                Guid verificationId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new UploadVerificationDocumentCommand(
                    VerificationId: verificationId,
                    MediaUploadId: request.MediaUploadId,
                    DocumentType: request.DocumentType);

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageVerification)
            .WithName(ApiEndpoint.Names.Me.UploadVerificationDocument)
            .WithTags(ApiEndpoint.Tags.Verifications);
    }
}

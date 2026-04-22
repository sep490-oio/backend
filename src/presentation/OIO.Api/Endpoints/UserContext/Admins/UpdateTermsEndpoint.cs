using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.UpdateTermsDocument;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public sealed class UpdateTermsEndpoint : IEndpoint
{
    public sealed record Request([Required] Guid MediaUploadId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Admins.UpdateTerms, async (
                Guid id,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new UpdateTermsDocumentCommand(
                    Id: id,
                    MediaUploadId: request.MediaUploadId);

                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageTerms)
            .WithName(ApiEndpoint.Names.Admins.UpdateTermsDocument)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<TermsDocumentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();
    }
}

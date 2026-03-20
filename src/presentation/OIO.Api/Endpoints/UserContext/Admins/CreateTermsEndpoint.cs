using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.CreateTermsDocument;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public sealed class CreateTermsDocumentEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Type,
        [Required] Guid MediaUploadId
    );
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.CreateTerms, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new CreateTermsDocumentCommand(
                    Type: request.Type,
                    MediaUploadId: request.MediaUploadId);
                
                var result = await sender.Send(command, ct);
                
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageTerms)
            .WithName(ApiEndpoint.Names.Admins.CreateTermsDocument)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<TermsDocumentDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}
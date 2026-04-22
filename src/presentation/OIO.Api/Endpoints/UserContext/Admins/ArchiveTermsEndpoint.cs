using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.ArchiveTermsDocument;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public sealed class ArchiveTermsEndpoint : IEndpoint
{
    public sealed record Request(string? Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.ArchiveTerms, async (
                Guid id,
                Request? request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ArchiveTermsDocumentCommand(
                    Id: id,
                    Reason: request?.Reason);

                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageTerms)
            .WithName(ApiEndpoint.Names.Admins.ArchiveTermsDocument)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<TermsDocumentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

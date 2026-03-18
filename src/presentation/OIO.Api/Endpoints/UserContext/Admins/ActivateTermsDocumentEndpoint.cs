using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.ActivateTermsDocument;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public sealed class ActivateTermsDocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Admins.ActivateTerms, async (
                Guid id,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new ActivateTermsDocumentCommand(id), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageTerms)
            .WithName(ApiEndpoint.Names.Admins.ActivateTermsDocument)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

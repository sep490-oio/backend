using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Queries.GetAllTermsDocuments;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public sealed class GetAllTermsDocumentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetAllTerms, async (
                string? type,
                bool? isActive,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetAllTermsDocumentsQuery(type, isActive), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadTerms)
            .WithName(ApiEndpoint.Names.Admins.GetAllTermsDocuments)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<IReadOnlyList<TermsDocumentDto>>(StatusCodes.Status200OK);
    }
}

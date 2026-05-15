using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetAdminItemQuestions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class GetAdminItemQuestionsEndpoint : IEndpoint
{
    public sealed record Parameters : GetAdminItemQuestionsFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetAdminItemQuestions, async (
                Guid itemId,
                [AsParameters] GetAdminItemQuestionsFilterParameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetAdminItemQuestionsQuery(itemId, parameters);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.GetAdminItemQuestions)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

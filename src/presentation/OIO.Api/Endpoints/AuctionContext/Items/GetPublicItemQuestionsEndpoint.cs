using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetPublicItemQuestions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class GetPublicItemQuestionsEndpoint : IEndpoint
{
    public sealed record Parameters : GetPublicItemQuestionsFilterParameters;
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Items.GetQuestions, async (
                Guid itemId,
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetPublicItemQuestionsQuery(itemId, parameters);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Items.GetItemQuestions)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
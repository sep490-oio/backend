using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetItemQuestions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class GetItemQuestionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Items.GetQuestions, async (
                Guid itemId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetItemQuestionsQuery(itemId);

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
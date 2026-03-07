using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.AskQuestion;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class AskQuestionEndpoint : IEndpoint
{
    public sealed record Request(string Question);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.AskQuestion, async (
                Guid itemId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new AskQuestionCommand(itemId, request.Question);

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Items.AskItemQuestion)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}
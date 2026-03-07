using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.AnswerQuestion;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class AnswerQuestionEndpoint : IEndpoint
{
    public sealed record Request(string Answer);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.AnswerQuestion, async (
                Guid itemId,
                Guid questionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new AnswerQuestionCommand(
                    itemId, 
                    questionId,
                    request.Answer);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Items.AnswerItemQuestion)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
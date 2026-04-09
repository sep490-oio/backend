using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.CreateAuctionDispute;
using OIO.Application.Context.ModerationContext.DTOs;

namespace OIO.Api.Endpoints.ModerationContext.Disputes;

public sealed class CreateAuctionDisputeEndpoint : IEndpoint
{
    public sealed record Request(string Domain, string CaseType, string Title, string Description);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Disputes.CreateAuctionDispute, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CreateAuctionDisputeCommand(
                        auctionId,
                        request.Domain,
                        request.CaseType,
                        request.Title,
                        request.Description),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Disputes.CreateAuctionDispute)
            .WithTags(ApiEndpoint.Tags.Disputes)
            .Produces<DisputeIntakeDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

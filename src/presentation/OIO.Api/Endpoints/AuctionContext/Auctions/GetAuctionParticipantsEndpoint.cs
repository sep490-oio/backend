using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Queries.GetAuctionParticipants;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

internal sealed class GetAuctionParticipantsEndpoint : IEndpoint
{
    public sealed record Parameters : GetAuctionParticipantsFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Auctions.GetParticipants, async (
                    Guid auctionId, 
                    [AsParameters] Parameters p, 
                    ISender sender, 
                    CancellationToken ct) =>
                    (await sender.Send(new GetAuctionParticipantsQuery(auctionId, p), ct)).ToOkHttpResult())
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Auctions.GetAuctionParticipants)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .WithSummary("Get all participants of an auction.")
            .Produces<PagedList<AuctionParticipantListItemDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}

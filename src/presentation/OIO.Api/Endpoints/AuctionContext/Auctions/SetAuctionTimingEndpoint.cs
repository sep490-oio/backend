using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.SetAuctionTiming;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class SetAuctionTimingEndpoint : IEndpoint
{
    public sealed record Request(
        DateTime StartTime,
        DateTime EndTime,
        DateTime QualificationStartAt,
        DateTime QualificationEndAt,
        bool AutoExtend = true,
        int ExtensionMinutes = 5);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Auctions.SetTiming, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new SetAuctionTimingCommand(
                    AuctionId: auctionId,
                    StartTime: request.StartTime,
                    EndTime: request.EndTime,
                    QualificationStartAt: request.QualificationStartAt,
                    QualificationEndAt: request.QualificationEndAt,
                    AutoExtend: request.AutoExtend,
                    ExtensionMinutes: request.ExtensionMinutes);

                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Create)
            .WithName(ApiEndpoint.Names.Auctions.SetAuctionTiming)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}

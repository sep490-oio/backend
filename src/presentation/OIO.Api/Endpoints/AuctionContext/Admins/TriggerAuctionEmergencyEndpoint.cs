using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.TriggerAuctionEmergency;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class TriggerAuctionEmergencyEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string TriggerSource, 
        [Required] string Reason, 
        [Required] object Payload);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.TriggerAuctionEmergency, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new TriggerAuctionEmergencyCommand(
                    AuctionId: auctionId,
                    TriggerSource: request.TriggerSource,
                    Reason: request.Reason,
                    Payload: request.Payload);

                var result = await sender.Send(command, ct);
                
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.TriggerAuctionEmergency)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}

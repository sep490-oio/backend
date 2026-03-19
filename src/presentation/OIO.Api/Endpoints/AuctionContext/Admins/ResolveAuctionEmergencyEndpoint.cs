using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.ResolveAuctionEmergency;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class ResolveAuctionEmergencyEndpoint : IEndpoint
{
    public sealed record Request([Required]string Status, [Required] object Payload);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.ResolveAuctionEmergency, async (
                Guid auctionId,
                Guid emergencyId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ResolveAuctionEmergencyCommand(
                    AuctionId: auctionId,
                    EmergencyId: emergencyId,
                    Status: request.Status,
                    Payload: request.Payload);

                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.ResolveAuctionEmergency)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}

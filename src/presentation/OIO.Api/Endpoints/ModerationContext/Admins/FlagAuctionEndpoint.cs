using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.CreateAuctionAlert;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class FlagAuctionEndpoint : IEndpoint
{
    public sealed record Request(string AlertType, string Severity = "medium", string Payload = "{}");

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.FlagAuction, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CreateAuctionAlertCommand(auctionId, request.AlertType, request.Severity, request.Payload),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.FlagAuction)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<MonitoringAlertDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

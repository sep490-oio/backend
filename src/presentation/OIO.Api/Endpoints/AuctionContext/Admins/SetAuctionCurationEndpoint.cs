using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.SetAuctionCuration;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class SetAuctionCurationEndpoint : IEndpoint
{
    public sealed record Request(
        Guid? AssignedAdminId = null,
        bool ClearAssignedAdmin = false,
        decimal? Priority = null,
        string? PriorityReason = null,
        bool? IsFeatured = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Admins.SetAuctionCuration, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new SetAuctionCurationCommand(
                    AuctionId: auctionId,
                    AssignedAdminId: request.AssignedAdminId,
                    ClearAssignedAdmin: request.ClearAssignedAdmin,
                    Priority: request.Priority,
                    PriorityReason: request.PriorityReason,
                    IsFeatured: request.IsFeatured);

                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.SetAuctionCuration)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}

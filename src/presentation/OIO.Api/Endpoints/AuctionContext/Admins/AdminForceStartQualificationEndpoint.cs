using OIO.Application.Context.AuctionContext.Commands.Admin.AdminForceStartQualification;
using OIO.Api.Common;
using OIO.Domain.AppDefinitions;
using MediatR;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class AdminForceStartQualificationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.AdminForceStartQualification, async (
                Guid auctionId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new AdminForceStartQualificationCommand(auctionId),
                    cancellationToken);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.AdminForceStartQualification)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

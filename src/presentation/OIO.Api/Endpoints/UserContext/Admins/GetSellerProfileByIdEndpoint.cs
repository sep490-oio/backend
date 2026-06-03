using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Queries.GetAdminSellerProfileById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class GetSellerProfileByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetSellerProfileById, async (
                Guid id,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetAdminSellerProfileByIdQuery(id);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageSellerProfiles)
            .WithName(ApiEndpoint.Names.Admins.GetSellerProfileById)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<AdminSellerProfileDetailDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);
    }
}

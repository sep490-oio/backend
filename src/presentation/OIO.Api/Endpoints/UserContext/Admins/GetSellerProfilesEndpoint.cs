using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Queries.GetSellerProfiles;
using OIO.Domain.AppDefinitions;
using OpenTelemetry.Trace;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class GetSellerProfilesEndpoint : IEndpoint
{
    public sealed record Parameters : GetSellerProfilesQueryFilter;
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetSellerProfiles, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetSellerProfilesQuery(parameters);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadSellerProfiles)
            .WithName(ApiEndpoint.Names.Admins.GetSellerProfiles)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<PagedList<SellerProfileDto>>(StatusCodes.Status200OK);
    }
}

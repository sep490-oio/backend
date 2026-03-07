using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.Filters;
using OIO.Application.Context.UserContext.Queries.GetUsers;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public sealed class GetUsersEndpoint : IEndpoint
{
    public sealed record Parameters : UserFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetUsers, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetUsersQuery(parameters);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadUsers)
            .WithName(ApiEndpoint.Names.Admins.GetUsers)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}
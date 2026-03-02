using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.UserContext.Queries.GetUserAddresses;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Users;

public class GetAddressesEndpoint : IEndpoint
{
    public sealed record Parameters : PagedParameters;
    
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Users.GetAddresses, async ([AsParameters] Parameters parameters, ISender sender, CancellationToken ct) =>
            {
                var query = new GetUserAddressesQuery(parameters);
                
                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.ReadAddresses)
            .WithName(ApiEndpoint.Names.Users.GetAddresses)
            .WithTags(ApiEndpoint.Tags.Users);
    }
}
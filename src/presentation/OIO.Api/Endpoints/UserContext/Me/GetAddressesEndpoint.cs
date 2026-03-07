using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.UserContext.Queries.GetUserAddresses;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class GetAddressesEndpoint : IEndpoint
{
    public sealed record Parameters : PagedParameters;
    
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetAddresses, async ([AsParameters] Parameters parameters, ISender sender, CancellationToken ct) =>
            {
                var query = new GetUserAddressesQuery(parameters);
                
                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadAddress)
            .WithName(ApiEndpoint.Names.Me.GetAddresses)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}
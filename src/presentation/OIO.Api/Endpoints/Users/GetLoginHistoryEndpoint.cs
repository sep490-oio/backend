using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.UserContext.Queries.GetLoginHistory;
using OIO.Domain.Constants.AppPermissions;

namespace OIO.Api.Endpoints.Users;

public class GetLoginHistoryEndpoint : IEndpoint
{
    public sealed record Parameters : PagedParameters;
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/users/me/login-history",
                async ([AsParameters] Parameters parameters, ISender sender,CancellationToken ct = default) =>
                {
                    var query = new GetLoginHistoryQuery(parameters);
                    
                    var result = await sender.Send(query, ct);

                    return result.IsFailure ? result.Error.ProcessError() : Results.Ok(result.Value);
                })
            .RequireAuthorization(AppPermission.Users.ReadLoginHistory)
            .WithTags(Tags.Users);
    }
}
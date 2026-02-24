using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.UserContext.Queries.GetLoginHistory;
using OIO.Domain.Constants.AppPermissions;

namespace OIO.Api.Endpoints.Users;

public class GetLoginHistoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/users/me/login-history",
                async ([AsParameters] PagedParameters parameter, ISender sender,CancellationToken ct = default) =>
                {
                    var result = await sender.Send(new GetLoginHistoryQuery(parameter), ct);

                    return result.IsFailure ? result.Error.ProcessError() : Results.Ok(result.Value);
                })
            .RequireAuthorization(AppPermission.Users.ReadLoginHistory)
            .WithTags(Tags.Users);
    }
}
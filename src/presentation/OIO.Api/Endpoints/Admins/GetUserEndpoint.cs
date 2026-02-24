using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Queries.GetUserById;

namespace OIO.Api.Endpoints.Admins;

public class GetUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/admin/users/{userId:guid}", async (Guid userId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetUserByIdQuery(userId), ct);

                return result.IsFailure ? 
                    result.Error.ProcessError() : 
                    Results.Ok(result.Value);
            })
            .AllowAnonymous()
            .WithTags(Tags.Admins);
    }
}
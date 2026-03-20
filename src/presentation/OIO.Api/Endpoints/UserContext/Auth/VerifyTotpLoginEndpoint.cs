using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Context.UserContext.Commands.VerifyTotpLogin;

namespace OIO.Api.Endpoints.UserContext.Auth;

public class VerifyTotpLoginEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Code, 
        [Required]  Guid DeviceId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auth.VerifyTotpLogin, async (
                Request request,
                ISender sender,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var command = new VerifyTotpLoginCommand(
                    Code: request.Code,
                    DeviceId: request.DeviceId,
                    IpAddress: httpContext.GetIpAddress(),
                    UserAgent: httpContext.Request.Headers.UserAgent.ToString());

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Auth.VerifyTotpLogin)
            .WithTags(ApiEndpoint.Tags.Auth);
    }
}

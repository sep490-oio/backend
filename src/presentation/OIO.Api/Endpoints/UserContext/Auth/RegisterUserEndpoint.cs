using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.RegisterUser;

namespace OIO.Api.Endpoints.UserContext.Auth;

public class RegisterUserEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string UserName,
        [Required] string Email,
        [Required] string Password,
        string? FirstName = null,
        string? LastName = null);
    
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auth.Register, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new RegisterUserCommand(
                    request.UserName,
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName);
                
                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Auth.Register)
            .WithTags(ApiEndpoint.Tags.Auth);
    }
}
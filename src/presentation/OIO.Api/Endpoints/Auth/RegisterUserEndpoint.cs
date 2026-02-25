using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.RegisterUser;

namespace OIO.Api.Endpoints.Auth;

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
        app.MapPost("api/auth/register", async (Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new RegisterUserCommand(
                    request.UserName,
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName);
                
                var result = await sender.Send(command, ct);

                return result.IsFailure ? 
                    result.Error.ProcessError() : 
                    Results.Created($"/api/auth/register", result.Value);
            })
            .AllowAnonymous()
            .WithTags(Tags.Auth);
    }
}
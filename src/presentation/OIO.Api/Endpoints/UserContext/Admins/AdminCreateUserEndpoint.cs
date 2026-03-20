using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.AdminCreateUser;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public sealed class AdminCreateUserEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string UserName,
        [Required] string Email,
        string? Password,
        [Required] string Currency,
        [Required] string FirstName,
        [Required] string LastName,
        string? DisplayName,
        IReadOnlyList<string>? Roles,
        bool EmailConfirmed = false,
        bool SkipNotifications = false);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.CreateUser, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new AdminCreateUserCommand(
                    UserName: request.UserName,
                    Email: request.Email,
                    Password: request.Password,
                    Currency: request.Currency,
                    FirstName: request.FirstName,
                    LastName: request.LastName,
                    DisplayName: request.DisplayName,
                    Roles: request.Roles,
                    EmailConfirmed: request.EmailConfirmed,
                    SkipNotifications: request.SkipNotifications);

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageUsers)
            .WithName(ApiEndpoint.Names.Admins.CreateUser)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<AdminUserCreatedDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}

using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.CreateUserRiskFlag;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class FlagUserEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string FlagType,
        string? Reason = null,
        string Severity = "medium");

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.FlagUser, async (
                Guid userId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CreateUserRiskFlagCommand(userId, request.FlagType, request.Reason, request.Severity),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageUsers)
            .WithName(ApiEndpoint.Names.Admins.FlagUser)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<UserRiskFlagDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

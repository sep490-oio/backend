using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.CreateCategory;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Categories;

public sealed class CreateCategoryEndpoint : IEndpoint
{
    public sealed record Request(
        string Name,
        string Slug,
        Guid? ParentId = null,
        string? Description = null,
        string? IconUrl = null,
        int SortOrder = 0);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Categories.Create, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new CreateCategoryCommand(
                    request.Name,
                    request.Slug,
                    request.ParentId,
                    request.Description,
                    request.IconUrl,
                    request.SortOrder);

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Categories.Create)
            .WithName(ApiEndpoint.Names.Categories.CreateCategory)
            .WithTags(ApiEndpoint.Tags.Categories)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}
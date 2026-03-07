using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.UpdateCategory;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Categories;

public sealed class UpdateCategoryEndpoint : IEndpoint
{
    public sealed record Request(
        string? Name = null,
        string? Slug = null,
        string? Description = null,
        string? IconUrl = null,
        bool? IsActive = null,
        int? SortOrder = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Categories.Update, async (
                Guid categoryId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new UpdateCategoryCommand(
                    categoryId,
                    request.Name,
                    request.Slug,
                    request.Description,
                    request.IconUrl,
                    request.IsActive,
                    request.SortOrder);

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Categories.Update)
            .WithName(ApiEndpoint.Names.Categories.UpdateCategory)
            .WithTags(ApiEndpoint.Tags.Categories)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
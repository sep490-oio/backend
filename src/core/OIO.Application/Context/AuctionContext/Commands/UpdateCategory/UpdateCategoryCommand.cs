using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Categories;
using OIO.Domain.Context.CatalogContext.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using CategoryId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.CategoryId;

namespace OIO.Application.Context.AuctionContext.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string? Name = null,
    string? Slug = null,
    string? Description = null,
    string? IconUrl = null,
    bool? IsActive = null,
    int? SortOrder = null) : ICommand<CategoryDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return UpdateCategoryCommand.Check()
            .WithOwnerName("UpdateCategory")
            .Field(CategoryId)
            .NotEmptyGuid()
            .Field(Name)
            .WhenHasValue(x =>
                x.NotWhiteSpace()
                .MaxLength(100))
            .Field(Slug)
            .WhenHasValue(x =>
                x.NotWhiteSpace()
                .MaxLength(100)
                .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            .Field(Description)
            .WhenHasValue(x =>
                x.NotWhiteSpace()
                .MaxLength(500))
            .Field(IconUrl)
            .WhenHasValue(x =>
                x.NotWhiteSpace()
                .MaxLength(500))
            .Field(SortOrder)
            .WhenHasValue(x => x.GreaterThanOrEqual(0));
    }
}

internal sealed class UpdateCategoryCommandHandler
    : ICommandHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateCategoryCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CategoryDto, Error>> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var categoryId = CategoryId.From(request.CategoryId);
        var category = await _dbContext.GetByIdAsync<Category, CategoryId>(
            id: categoryId,
            cancellationToken: cancellationToken);

        if (category is null)
            return AuctionErrors.Category.NotFound(categoryId);

        // Check slug uniqueness if changing
        Slug? slug = null;
        if (request.Slug is not null && request.Slug != category.Slug.Value)
        {
             (_, var isFailure, slug, var error) = Slug.Create(request.Slug);

            if (isFailure)
            {
                return error;
            }
            
            var slugExists = await _dbContext.Set<Category>()
                .AnyAsync(x => x.Slug.Value == slug.Value && x.Id != categoryId, cancellationToken);

            if (slugExists)
                return AuctionErrors.Category.SlugAlreadyExists(slug.Value);
        }

        category.Update(
            nowUtc: _clock.UtcNow,
            name: request.Name,
            slug: slug,
            description: request.Description,
            isActive: request.IsActive,
            sortOrder: request.SortOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.ToDto();
    }
}
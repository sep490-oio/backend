using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Categories;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    Guid? ParentId = null,
    string? Description = null,
    string? IconUrl = null,
    int SortOrder = 0) : ICommand<CategoryDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return CreateCategoryCommand.Check()
            .WithOwnerName("CreateCategory")
            .Field(Name)
            .NotWhiteSpace()
            .MaxLength(100)
            .Field(Slug)
            .NotWhiteSpace()
            .MaxLength(100)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .Field(Description)
            .WhenHasValue( x =>
                x.NotWhiteSpace()
                .MaxLength(500))
            .Field(IconUrl)
            .WhenHasValue(x =>
                x.NotWhiteSpace()
                .MaxLength(500))
            .Field(SortOrder)
            .GreaterThanOrEqual(0)
            .Field(ParentId)
            .WhenHasValue(x => x.NotEmptyGuid());
    }
}

internal sealed class CreateCategoryCommandHandler
    : ICommandHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateCategoryCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CategoryDto, Error>> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        // Check slug uniqueness
        var slugExists = await _dbContext.Set<Category>()
            .AnyAsync(c => c.Slug == normalizedSlug, cancellationToken);
            
        if (slugExists)
            return AuctionErrors.Category.SlugAlreadyExists(request.Slug);

        // Validate parent
        Category? parent = null;
        if (request.ParentId.HasValue)
        {
            var categoryId = CategoryId.From(request.ParentId.Value);
            parent = await _dbContext.GetByIdAsync<Category, CategoryId>(
                id: categoryId,
                cancellationToken: cancellationToken
            );

            if (parent is null)
                return AuctionErrors.Category.NotFound(categoryId);
        }

        CategoryPath? path;
        if (parent is null)
        {
            (_, var  isFailure, path, var  error) = CategoryPath.Create(request.Slug);
            
            if (isFailure)
                return error;
        }
        else
        {
            (_, var  isFailure, path, var  error)  = CategoryPath.FromParent(parent.Path, request.Slug);
            
            if (isFailure)
                return error;
        }
        var nowUtc = _clock.UtcNow;

        var category = Category.Create(
            name: request.Name,
            slug: request.Slug,
            nowUtc: nowUtc,
            parentPath: path,
            parentId: parent?.Id,
            description: request.Description,
            iconUrl: request.IconUrl,
            sortOrder: request.SortOrder);

        _dbContext.Insert(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.ToDto();
    }
}
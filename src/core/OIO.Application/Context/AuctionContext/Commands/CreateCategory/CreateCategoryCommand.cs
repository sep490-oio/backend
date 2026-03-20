using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.MediaContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Categories;
using OIO.Domain.Context.CatalogContext.ValueObjects;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using CategoryId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.CategoryId;

namespace OIO.Application.Context.AuctionContext.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    Guid? ParentId = null,
    string? Description = null,
    Guid? MediaUploadId = null,
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
            .Field(MediaUploadId)
            .WhenHasValue(x =>
                x.NotEmptyGuid())
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
    private readonly UploadContextRegistry _contextRegistry;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IMediaRelocationService _mediaRelocationService;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        UploadContextRegistry contextRegistry,
        ICurrentUser currentUser,
        IClock clock,
        IMediaRelocationService mediaRelocationService,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _contextRegistry = contextRegistry;
        _currentUser = currentUser;
        _clock = clock;
        _mediaRelocationService = mediaRelocationService;
        _logger = logger;
    }

    public async Task<Result<CategoryDto, Error>> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        // Check slug uniqueness
        var slugExists = await _dbContext.Set<Category>()
            .AnyAsync(c => c.Slug.Value == normalizedSlug, cancellationToken);
            
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
        
        var (_, isFailure, slug, error) = Slug.Create(request.Slug);

        if (isFailure)
        {
            return error;
        }

        CategoryPath? path;
        if (parent is null)
        {
            (_, isFailure, path, error) = CategoryPath.Create(request.Slug);
            
            if (isFailure)
                return error;
        }
        else
        {
            path = CategoryPath.FromParent(parent.Path, slug);
        }
        
        
        var nowUtc = _clock.UtcNow;

        
        var category = Category.Create(
            name: request.Name,
            slug: slug,
            nowUtc: nowUtc,
            parentPath: path,
            parentId: parent?.Id,
            description: request.Description,
            sortOrder: request.SortOrder);

        MediaUpload? linkedUpload = null;
        if(request.MediaUploadId.HasValue)
        {
            var mediaUploadId = MediaUploadId.From(request.MediaUploadId.Value);
            var upload = await _dbContext.GetByIdAsync<MediaUpload, MediaUploadId>(
                id: mediaUploadId,
                cancellationToken: cancellationToken);

            if (upload is null)
                return MediaErrors.NotFound(mediaUploadId);

            if (upload.UserId != _currentUser.UserId)
            {
                _logger.LogWarning("User {UserId} attempted to use media upload {MediaUploadId} which they do not own.",
                    _currentUser.UserId, mediaUploadId);
                return MediaErrors.NotOwnedByUser(mediaUploadId);
            }

            if (!upload.IsConfirmed)
                return MediaErrors.NotConfirm;

            if (upload.IsLinked)
                return MediaErrors.AlreadyLinked;

            if (!_contextRegistry.IsCategoryContext(upload.Context))
            {
                _logger.LogWarning("Media upload {MediaUploadId} has invalid context {Context} for category {category}.",
                    mediaUploadId, upload.Context, category.Id);
                return MediaErrors.WrongContext(upload.Context, _contextRegistry.GetAllContext());
            }
            
            category.Update(
                nowUtc: nowUtc, 
                iconInfo: upload?.Info,
                iconStorage: upload?.StorageRef);

            upload!.LinkToEntity(category.Id, nowUtc);
            linkedUpload = upload;
        }
        
        _dbContext.Insert(category);

        if (linkedUpload is not null)
            await _mediaRelocationService.RelocateLinkedUploadAsync(linkedUpload, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.ToDto();
    }
}


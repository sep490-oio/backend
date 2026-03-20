using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.MediaContext.Services;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.CreateTermsDocument;

internal sealed class CreateTermsDocumentCommandHandler : ICommandHandler<CreateTermsDocumentCommand, TermsDocumentDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly UploadContextRegistry _contextRegistry;
    private readonly IMediaRelocationService _mediaRelocationService;
    private readonly ILogger<CreateTermsDocumentCommandHandler> _logger;

    public CreateTermsDocumentCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        UploadContextRegistry contextRegistry,
        IMediaRelocationService mediaRelocationService,
        ILogger<CreateTermsDocumentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _contextRegistry = contextRegistry;
        _mediaRelocationService = mediaRelocationService;
        _logger = logger;
    }

    public async Task<Result<TermsDocumentDto, Error>> Handle(CreateTermsDocumentCommand request, CancellationToken cancellationToken)
    {
        var mediaUploadId = MediaUploadId.From(request.MediaUploadId);
        var mediaUpload = await _dbContext.GetByIdAsync<MediaUpload, MediaUploadId>(
            id: mediaUploadId,
            cancellationToken: cancellationToken);

        if (mediaUpload is null)
        {
            return MediaErrors.NotFound(mediaUploadId);
        }
        
        var validationError = await ValidatePendingUploadsAsync(
            mediaUpload, 
            _currentUser.UserId,
            cancellationToken);

        if (validationError is not null)
            return validationError;
        
        var normalizedType = request.Type.Trim();

        var currentMaxVersion = await _dbContext.Set<TermsDocument>()
            .Where(x => x.TermType.ToLower() == normalizedType.ToLower())
            .Select(x => (int?)x.Version)
            .MaxAsync(cancellationToken) ?? 0;


        var documentResult = TermsDocument.Create(
            normalizedType,
            currentMaxVersion + 1,
            mediaUpload,
            _clock.UtcNow);

        if (documentResult.IsFailure)
            return documentResult.Error;

        var document = documentResult.Value;
        
        _dbContext.Insert(document);

        await _mediaRelocationService.RelocateLinkedUploadAsync(mediaUpload, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return document.ToDto();
    }
    
    private async Task<Error?> ValidatePendingUploadsAsync(
        MediaUpload found,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        // Check ownership
        if (found.UserId != userId)
        {
            _logger.LogWarning("Media uploads not owned by user {UserId}: {NotOwnedIds}",
                    userId, found.Id);
            return MediaErrors.NotOwnedByUser(found.Id);
        }

        if (!found.IsConfirmed)
        {
            _logger.LogWarning("Media uploads not confirmed: {UnconfirmedIds}", found.Id);
            return MediaErrors.NotConfirm;
        }
       
        if (found.IsLinked)
        {
            _logger.LogWarning("Media uploads already linked: {AlreadyLinkedIds}", found.Id);
            return MediaErrors.AlreadyLinked;
        }
        
        if (!_contextRegistry.IsTermContext(found.Context))
        {
            _logger.LogWarning("Media uploads invalid context: {InvalidContext}", found.Id);
            return MediaErrors.WrongContext(found.Context, _contextRegistry.GetAllContext());
        }
        return null;
    }
}


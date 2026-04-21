using CSharpFunctionalExtensions;
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
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.UpdateTermsDocument;

/// <summary>
/// Swaps the PDF on a <c>Draft</c> terms document (plan B2 / §3.3 <c>UpdateDraftContent</c>).
/// Version is NOT incremented — only <c>Activate</c> commits a new version (Q3 decision §6).
/// </summary>
internal sealed class UpdateTermsDocumentCommandHandler
    : ICommandHandler<UpdateTermsDocumentCommand, TermsDocumentDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly UploadContextRegistry _contextRegistry;
    private readonly IMediaRelocationService _mediaRelocationService;
    private readonly ILogger<UpdateTermsDocumentCommandHandler> _logger;

    public UpdateTermsDocumentCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        UploadContextRegistry contextRegistry,
        IMediaRelocationService mediaRelocationService,
        ILogger<UpdateTermsDocumentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _contextRegistry = contextRegistry;
        _mediaRelocationService = mediaRelocationService;
        _logger = logger;
    }

    public async Task<Result<TermsDocumentDto, Error>> Handle(
        UpdateTermsDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var termsId = TermsDocumentId.From(request.Id);
        var target = await _dbContext.GetByIdAsync<TermsDocument, TermsDocumentId>(
            termsId, cancellationToken: cancellationToken);

        if (target is null)
            return TermsErrors.TermsDocumentNotFound(termsId);

        if (target.Status != TermsDocumentStatus.Draft)
        {
            return Error.Conflict(
                "TermsDocument.InvalidState",
                $"Only draft terms documents can be edited (current status: '{target.Status.Id}').");
        }

        var mediaUploadId = MediaUploadId.From(request.MediaUploadId);
        var mediaUpload = await _dbContext.GetByIdAsync<MediaUpload, MediaUploadId>(
            id: mediaUploadId,
            cancellationToken: cancellationToken);

        if (mediaUpload is null)
            return MediaErrors.NotFound(mediaUploadId);

        var validationError = ValidatePendingUpload(mediaUpload, _currentUser.UserId);
        if (validationError is not null)
            return validationError;

        var updateResult = target.UpdateDraftContent(mediaUpload, _clock.UtcNow);
        if (updateResult.IsFailure)
            return updateResult.Error;

        await _mediaRelocationService.RelocateLinkedUploadAsync(mediaUpload, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return target.ToDto();
    }

    private Error? ValidatePendingUpload(MediaUpload found, UserId userId)
    {
        if (found.UserId != userId)
        {
            _logger.LogWarning("Media upload not owned by user {UserId}: {UploadId}", userId, found.Id);
            return MediaErrors.NotOwnedByUser(found.Id);
        }

        if (!found.IsConfirmed)
        {
            _logger.LogWarning("Media upload not confirmed: {UploadId}", found.Id);
            return MediaErrors.NotConfirm;
        }

        if (found.IsLinked)
        {
            _logger.LogWarning("Media upload already linked: {UploadId}", found.Id);
            return MediaErrors.AlreadyLinked;
        }

        if (!_contextRegistry.IsTermContext(found.Context))
        {
            _logger.LogWarning("Media upload invalid context: {UploadId}", found.Id);
            return MediaErrors.WrongContext(found.Context, _contextRegistry.GetAllContext());
        }

        return null;
    }
}
